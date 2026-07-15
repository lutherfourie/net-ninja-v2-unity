using System.Collections.Generic;
using NetNinja.Contracts;
using NetNinja.Core.State;
using NetNinja.Core.SweepPolicy;
using NetNinja.Core.Telemetry;

namespace NetNinja.Core
{
    public sealed class Sim
    {
        public const double FixedDt = 1.0 / 60.0;

        public readonly CoreConfig Cfg;
        public readonly int Seed;
        public readonly TelemetrySink Telemetry = new TelemetrySink();
        public readonly NetCatcher Net;
        public readonly WaveManager Cat;
        public readonly ScoreManager Score;
        public readonly TempoDirector Tempo;
        public readonly CharmEffects Charms;
        public readonly List<FallingObject> Objects = new List<FallingObject>();
        public double Time;
        public double LastSpikeAt = double.NegativeInfinity;

        internal double ReturnStrokeStartedAt;
        internal bool WasFull;
        double _diagFullStart;
        int _diagMissAtFull;
        int _tick;

        public Sim(CoreConfig cfg, int seed)
        {
            Cfg = cfg;
            Seed = seed;
            Net = new NetCatcher(cfg, Telemetry);
            Score = new ScoreManager(cfg, Telemetry);
            Cat = new WaveManager(cfg, seed, PolicyRegistry.Make(cfg.S("net.sweep.policy")), Telemetry);
            Tempo = new TempoDirector(cfg);
            Charms = new CharmEffects(cfg, Telemetry);

            Telemetry.On(e =>
            {
                if (e.Name == "cat.sweep_begin")
                {
                    Score.TrackSweep(
                        (int)e.Payload.GetDouble("sweepId"),
                        e.Payload.GetDouble("count"),
                        e.Payload.GetDouble("stagger"),
                        e.T);
                }
                else if (e.Name == "cat.push_begin")
                {
                    Score.TrackTrail(
                        (int)e.Payload.GetDouble("trailId"),
                        e.Payload.GetDouble("count"),
                        e.T);
                }
                else if (e.Name == "net.rimhit")
                {
                    Score.CoolFromRim();
                    Tempo.OnRimClank();
                }
                else if (e.Name == "net.catch") Tempo.OnCatch();
                else if (e.Name == "net.wavecatch") Tempo.OnWaveCatch();
                else if (e.Name == "net.trailclear") Tempo.OnTrailClear();
                else if (e.Name == "net.dump") Tempo.OnDump();
                else if (e.Name == "net.miss") Tempo.OnMiss();
                else if (e.Name == "net.break")
                {
                    Score.Temper *= 1 - Cfg.D("temper.missCool");
                    Tempo.OnMiss();
                }
                else if (e.Name == "net.parry")
                {
                    Score.Temper = Fp.Min(1, Score.Temper + Cfg.D("temper.perCatch") * 2);
                }
                else if (e.Name == "charm.absorb")
                {
                    string effect = e.Payload.GetString("effect");
                    if (effect == "life")
                    {
                        if (Score.Lives > 0)
                        {
                            int cap = Cfg.I("run.lives");
                            Score.Lives = Score.Lives + 1 > cap ? cap : Score.Lives + 1;
                        }
                    }
                    else if (effect == "barrier" || effect == "slow")
                        Charms.Activate(effect, e.T);
                }
            });
            Telemetry.Emit(0, "run.start", new TelemetryPayload()
                .Set("seed", seed).Set("policy", cfg.S("net.sweep.policy")));
        }

        public double Derangement => Vec3.Clamp01(Time / Cfg.D("run.derangementRampSeconds"));

        /// <summary>Apply input then advance one fixed tick. Returns snapshot+hash.</summary>
        public StateSnapshot Tick(InputFrame input)
        {
            if (!Score.GameOver)
            {
                Net.Target.Set(input.TargetX, input.TargetY, 0);
                Net.PointerFollow = input.PointerFollow;
                Step();
            }
            _tick++;
            return Snapshot();
        }

        /// <summary>Core step without input (persona sets Net.Target beforehand, matching TS).</summary>
        public void Step()
        {
            if (Score.GameOver) return;
            double dt = FixedDt;
            Time += dt;
            double now = Time;
            Score.Tick(dt);
            Score.ExpireTrackers(now);
            Tempo.Tick(dt);

            if (Cfg.B("net.capacityFollowsPush"))
            {
                int wantA = Cfg.I("net.capacity");
                int wantB = PolicyRegistry.ItemsPerPushNow(Cfg, Derangement);
                int want = wantA > wantB ? wantA : wantB;
                if (Net.DynamicCapacity == null || want > Net.DynamicCapacity.Value)
                    Net.DynamicCapacity = want;
            }

            TempoDirector tempoArg = Cfg.B("tempo.director.enabled") ? Tempo : null;
            Cat.Step(now, dt, Derangement, Net.FreeSlots, Net.Capacity, tempoArg, o => Objects.Add(o));

            Net.SimNow = Time;
            Net.Step(dt);
            Charms.Tick(now);

            if (Net.IsFull && !WasFull)
            {
                ReturnStrokeStartedAt = now;
                if (Cfg.B("diag.telemetry"))
                {
                    _diagFullStart = now;
                    _diagMissAtFull = Score.Misses;
                    Telemetry.Emit(now, "net.full_enter", new TelemetryPayload().Set("heldCount", Net.Held.Count));
                }
            }
            if (!Net.IsFull && WasFull && Cfg.B("diag.telemetry"))
            {
                Telemetry.Emit(now, "net.full_exit", new TelemetryPayload()
                    .Set("fullMs", Fp.RoundToIntNonNeg((now - _diagFullStart) * 1000))
                    .Set("missesDuring", Score.Misses - _diagMissAtFull));
            }
            WasFull = Net.IsFull;

            Net.TryPour(now, ReturnStrokeStartedAt);

            double? pendingSpikeX = null, pendingSpikeY = null;

            for (int oi = 0; oi < Objects.Count; oi++)
            {
                var item = Objects[oi];
                if (item.State != FallState.Falling) continue;
                if (item.Banked)
                {
                    var st = item.Step(dt, Cfg.D("world.fallGravity"), Cfg.D("world.binY"));
                    if (st == FallState.Landed)
                        Score.RegisterDump(new List<FallingObject> { item }, now, false, Derangement);
                    continue;
                }
                double prevItemX = item.Pos.X, prevItemY = item.Pos.Y;
                if (Cfg.B("net.magnet.enabled")) Net.ApplyMagnet(item, dt);
                double useDt = dt;
                if (Charms.SlowActive && !item.KoUntilGone)
                {
                    double dx = item.Pos.X - Net.Pos.X;
                    double dy = item.Pos.Y - Net.Pos.Y;
                    double R = Cfg.D("charm.slow.radius");
                    if (dx * dx + dy * dy <= R * R)
                        useDt = dt * Cfg.D("charm.slow.factor");
                }
                var state = item.Step(useDt, Cfg.D("world.fallGravity"), Cfg.D("world.floorY"));
                if (state != FallState.Landed && item.State == FallState.Falling)
                {
                    double catchSpeed = Fp.Hypot2(item.VelX, item.VelY);
                    if (Net.TryCatch(item, now, prevItemX, prevItemY, dt))
                    {
                        Score.RegisterCatch(item, now);
                        if (Cfg.B("net.spike.enabled")
                            && item.Rungs >= Cfg.I("net.spike.minRungs")
                            && catchSpeed >= Cfg.D("net.spike.minCatchSpeed")
                            && now - LastSpikeAt >= Cfg.D("net.spike.cooldownSec"))
                        {
                            pendingSpikeX = item.Pos.X;
                            pendingSpikeY = item.Pos.Y;
                            LastSpikeAt = now;
                        }
                        continue;
                    }
                    if (Cfg.B("diag.telemetry") && Net.IsGhostPass(item, prevItemX, prevItemY))
                    {
                        Net.MouthPlaneCrossHit(item, prevItemX, prevItemY, dt, out var hit);
                        Telemetry.Emit(now, "diag.ghost_pass", new TelemetryPayload()
                            .Set("itemId", item.Id)
                            .Set("itemX", hit.ItemX)
                            .Set("itemY", hit.ItemY)
                            .Set("netSpeed", Fp.Sqrt(Net.VelX * Net.VelX + Net.VelY * Net.VelY))
                            .Set("lat", hit.Lat)
                            .Set("sweepId", item.SweepId));
                    }
                }
                double wall = Cfg.D("world.halfWidth") - item.Radius;
                if (!item.KoUntilGone)
                {
                    if (item.Pos.X > wall)
                    {
                        item.Pos.X = wall;
                        item.VelX = -Fp.Abs(item.VelX) * Cfg.D("world.wallRestitution");
                    }
                    else if (item.Pos.X < -wall)
                    {
                        item.Pos.X = -wall;
                        item.VelX = Fp.Abs(item.VelX) * Cfg.D("world.wallRestitution");
                    }
                }
                bool xCull = Fp.Abs(item.Pos.X) > Cfg.D("world.halfWidth") + 2;
                if (item.KoUntilGone && xCull) continue;
                if (state == FallState.Landed)
                {
                    if (item.Kind == ItemKind.Knife)
                    {
                        Telemetry.Emit(now, "knife.floored", new TelemetryPayload().Set("x", item.Pos.X));
                        continue;
                    }
                    if (item.KoUntilGone) continue;
                    if (Charms.BarrierActive)
                    {
                        item.State = FallState.Falling;
                        item.VelY = Cfg.D("charm.barrier.bounceVel");
                        item.Pos.Y = Cfg.D("world.floorY");
                        Charms.ConsumeBarrierCrack(now, item.Pos.X);
                        continue;
                    }
                    if (item.Kind == ItemKind.Charm)
                    {
                        Telemetry.Emit(now, "charm.lost", new TelemetryPayload().Set("x", item.Pos.X));
                        continue;
                    }
                    Score.RegisterMiss(item, now);
                    if (Cfg.B("diag.telemetry"))
                    {
                        int airborne = 0;
                        for (int q = 0; q < Objects.Count; q++)
                        {
                            var oq = Objects[q];
                            if (oq != item && oq.State == FallState.Falling && !oq.Banked) airborne++;
                        }
                        Telemetry.Emit(now, "diag.miss", new TelemetryPayload()
                            .Set("netFull", Net.IsFull)
                            .Set("heldCount", Net.Held.Count)
                            .Set("freeSlots", Net.FreeSlots)
                            .Set("airborne", airborne)
                            .Set("isSweep", item.SweepId != 0));
                    }
                }
            }

            var flicked = Net.TryFlickDump(now, ReturnStrokeStartedAt);
            if (flicked.Count > 0) Score.RegisterDump(flicked, now, true, Derangement);
            var dumped = Net.TryDump(now, ReturnStrokeStartedAt);
            if (dumped.Count > 0) Score.RegisterDump(dumped, now, false, Derangement);

            if (pendingSpikeX.HasValue)
            {
                double total = 0;
                int count = 0;
                for (int i = 0; i < Objects.Count; i++)
                {
                    var o = Objects[i];
                    if (o.State != FallState.Falling || o.Banked || o.KoUntilGone) continue;
                    if (o.Kind == ItemKind.Knife || o.Kind == ItemKind.Charm) continue;
                    total += o.Value;
                    Telemetry.Emit(now, "spike.bank", new TelemetryPayload()
                        .Set("x", o.Pos.X).Set("y", o.Pos.Y).Set("value", o.Value));
                    o.State = FallState.Landed;
                    count++;
                }
                Score.Score += total;
                Telemetry.Emit(now, "net.spike", new TelemetryPayload()
                    .Set("count", count).Set("points", total)
                    .Set("x", pendingSpikeX.Value).Set("y", pendingSpikeY.Value));
            }

            for (int i = Objects.Count - 1; i >= 0; i--)
            {
                var o = Objects[i];
                bool koCull = o.KoUntilGone && (o.State == FallState.Landed
                    || Fp.Abs(o.Pos.X) > Cfg.D("world.halfWidth") + 2);
                bool heldGone = o.State == FallState.Caught && !Net.Held.Contains(o);
                if (koCull || o.State == FallState.Landed || heldGone)
                    Objects.RemoveAt(i);
            }
        }

        public string HashState() => FnvStateHasher.HashState(this);

        public StateSnapshot Snapshot()
        {
            return new StateSnapshot
            {
                Tick = _tick,
                Time = Time,
                StateHash = HashState(),
                NetPos = Net.Pos,
                NetTarget = Net.Target,
                NetVelX = Net.VelX,
                NetVelY = Net.VelY,
                FacingX = Net.FacingX,
                FacingY = Net.FacingY,
                HeldCount = Net.Held.Count,
                Capacity = Net.Capacity,
                IsFull = Net.IsFull,
                Score = Score.Score,
                Combo = Score.Combo,
                Lives = Score.Lives,
                Misses = Score.Misses,
                WaveCatches = Score.WaveCatches,
                Temper = Score.Temper,
                Phase = WaveManager.PhaseName(Cat.Phase),
                CatX = Cat.CatX,
                CatIntensity = Tempo.CatIntensity,
                GameOver = Score.GameOver,
            };
        }
    }
}
