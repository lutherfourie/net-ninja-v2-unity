using System;
using System.Collections.Generic;
using NetNinja.Contracts;
using NetNinja.Core.SweepPolicy;
using NetNinja.Core.Telemetry;

namespace NetNinja.Core
{
    public enum WavePhase
    {
        Cooldown = 0,
        Walk = 1,
        Anticipate = 2,
        Telegraph = 3,
        Pushing = 4,
    }

    public sealed class TelegraphInfo
    {
        public double[] Xs;
        public int Direction;
        public double EndsAt;
    }

    public sealed class WaveManager
    {
        readonly CoreConfig _cfg;
        readonly int _seed;
        readonly ISweepPolicy _policy;
        readonly TelemetrySink _telemetry;

        public WavePhase Phase = WavePhase.Cooldown;
        public double CatX;
        public TelegraphInfo Telegraph;

        // hashed internals
        internal double PhaseEndsAt;
        internal double NextSpawnAt = 1.5;
        internal int CeremonyIndex;
        internal double LastSweepAt = -999;
        internal SweepDecision? Pending;
        internal int PushedInCeremony;
        internal double NextPushAt;
        internal int CurrentSweepId;
        internal int SweepSerial;
        internal int CurrentTrailId;
        internal int TrailSerial;
        internal int ObjectSerial;

        public WaveManager(CoreConfig cfg, int seed, ISweepPolicy policy, TelemetrySink telemetry)
        {
            _cfg = cfg;
            _seed = seed;
            _policy = policy;
            _telemetry = telemetry;
        }

        public static string PhaseName(WavePhase p)
        {
            switch (p)
            {
                case WavePhase.Cooldown: return "cooldown";
                case WavePhase.Walk: return "walk";
                case WavePhase.Anticipate: return "anticipate";
                case WavePhase.Telegraph: return "telegraph";
                case WavePhase.Pushing: return "pushing";
                default: return "cooldown";
            }
        }

        public void Step(double now, double dt, double derangement, int freeSlots, int totalCapacity,
            TempoDirector tempo, Action<FallingObject> spawn)
        {
            switch (Phase)
            {
                case WavePhase.Cooldown:
                {
                    if (now < NextSpawnAt) return;
                    Pending = _policy.Decide(new SweepContext
                    {
                        Seed = _seed,
                        CeremonyIndex = CeremonyIndex,
                        Derangement = derangement,
                        FreeSlots = freeSlots,
                        TotalCapacity = totalCapacity,
                        SimTime = now,
                        LastSweepAt = LastSweepAt,
                        NextSpawnX = RollSpawnX(),
                    }, _cfg);
                    Phase = WavePhase.Walk;
                    PhaseEndsAt = now + _cfg.D("ceremony.walkArrivalCapSeconds");
                    break;
                }
                case WavePhase.Walk:
                {
                    if (Pending.HasValue) CatX = Pending.Value.Xs[0];
                    if (now >= PhaseEndsAt)
                    {
                        Phase = WavePhase.Anticipate;
                        PhaseEndsAt = now + _cfg.D("ceremony.anticipateSeconds") * (1 - 0.5 * derangement);
                    }
                    break;
                }
                case WavePhase.Anticipate:
                {
                    if (now < PhaseEndsAt) return;
                    double tel = TelegraphSeconds(derangement);
                    Phase = WavePhase.Telegraph;
                    PhaseEndsAt = now + tel;
                    var p = Pending.Value;
                    Telegraph = new TelegraphInfo { Xs = p.Xs, Direction = p.Direction, EndsAt = PhaseEndsAt };
                    if (p.IsSweep)
                    {
                        CurrentSweepId = ++SweepSerial;
                        LastSweepAt = now;
                        _telemetry.Emit(now, "cat.sweep_begin", new TelemetryPayload()
                            .Set("count", p.Count)
                            .Set("span", Fp.Abs(p.Xs[p.Count - 1] - p.Xs[0]))
                            .Set("direction", p.Direction)
                            .Set("stagger", p.StaggerSeconds)
                            .Set("policy", _policy.Name)
                            .Set("sweepId", CurrentSweepId));
                    }
                    else if (p.Count > 1)
                    {
                        CurrentTrailId = ++TrailSerial;
                        _telemetry.Emit(now, "cat.push_begin", new TelemetryPayload()
                            .Set("count", p.Count)
                            .Set("trailId", CurrentTrailId)
                            .Set("direction", p.Direction));
                    }
                    break;
                }
                case WavePhase.Telegraph:
                {
                    if (now < PhaseEndsAt) return;
                    Phase = WavePhase.Pushing;
                    PushedInCeremony = 0;
                    NextPushAt = now;
                    break;
                }
                case WavePhase.Pushing:
                {
                    var p = Pending.Value;
                    int goldenIdx = -1, bouncyIdx = -1, knifeIdx = -1, charmIdx = -1;
                    string charmEffect = "barrier";
                    if (_cfg.B("items.taxonomy.enabled") && !p.IsSweep)
                    {
                        if (Rng.Hash01(_seed, CeremonyIndex * 31 + 41) < _cfg.D("items.goldenPushOdds"))
                        {
                            goldenIdx = (int)Fp.Min(p.Count - 1,
                                Fp.Floor(Rng.Hash01(_seed, CeremonyIndex * 31 + 42) * p.Count));
                        }
                        if (Rng.Hash01(_seed, CeremonyIndex * 31 + 43) < _cfg.D("items.bouncyPushChance"))
                        {
                            bouncyIdx = (int)Fp.Min(p.Count - 1,
                                Fp.Floor(Rng.Hash01(_seed, CeremonyIndex * 31 + 44) * p.Count));
                            if (bouncyIdx == goldenIdx)
                            {
                                if (p.Count == 1) bouncyIdx = -1;
                                else bouncyIdx = (bouncyIdx + 1) % p.Count;
                            }
                        }
                        if (Rng.Hash01(_seed, CeremonyIndex * 31 + 45) < _cfg.D("items.knifePushChance"))
                        {
                            int rawKnife = (int)Fp.Min(p.Count - 1,
                                Fp.Floor(Rng.Hash01(_seed, CeremonyIndex * 31 + 46) * p.Count));
                            var occupied = new HashSet<int>();
                            if (goldenIdx >= 0) occupied.Add(goldenIdx);
                            if (bouncyIdx >= 0) occupied.Add(bouncyIdx);
                            int idx = rawKnife;
                            bool placed = false;
                            for (int attempt = 0; attempt < p.Count; attempt++)
                            {
                                if (!occupied.Contains(idx))
                                {
                                    knifeIdx = idx;
                                    placed = true;
                                    break;
                                }
                                idx = (idx + 1) % p.Count;
                            }
                            if (!placed) knifeIdx = -1;
                        }
                        if (_cfg.B("items.charms.enabled")
                            && Rng.Hash01(_seed, CeremonyIndex * 31 + 47) < _cfg.D("items.charmPushChance"))
                        {
                            int rawCharm = (int)Fp.Min(p.Count - 1,
                                Fp.Floor(Rng.Hash01(_seed, CeremonyIndex * 31 + 48) * p.Count));
                            var occupied = new HashSet<int>();
                            if (goldenIdx >= 0) occupied.Add(goldenIdx);
                            if (bouncyIdx >= 0) occupied.Add(bouncyIdx);
                            if (knifeIdx >= 0) occupied.Add(knifeIdx);
                            int idx = rawCharm;
                            bool placed = false;
                            for (int attempt = 0; attempt < p.Count; attempt++)
                            {
                                if (!occupied.Contains(idx))
                                {
                                    charmIdx = idx;
                                    placed = true;
                                    break;
                                }
                                idx = (idx + 1) % p.Count;
                            }
                            if (!placed) charmIdx = -1;
                            else
                            {
                                double r = Rng.Hash01(_seed, CeremonyIndex * 31 + 49);
                                if (r < _cfg.D("charm.payout.lifeOdds")) charmEffect = "life";
                                else if (r < 0.55) charmEffect = "barrier";
                                else charmEffect = "slow";
                            }
                        }
                    }

                    while (PushedInCeremony < p.Count && now >= NextPushAt)
                    {
                        double x = p.Xs[PushedInCeremony];
                        int i = PushedInCeremony;
                        double value = 10;
                        ItemKind kind = ItemKind.Plain;
                        if (i == knifeIdx) { kind = ItemKind.Knife; value = 0; }
                        else if (i == goldenIdx) { kind = ItemKind.Golden; value = _cfg.D("items.goldenValue"); }
                        else if (i == bouncyIdx) { kind = ItemKind.Bouncy; }
                        else if (i == charmIdx) { kind = ItemKind.Charm; value = 0; }

                        var obj = new FallingObject(x, _cfg.D("world.shelfY"), _cfg.D("world.itemRadius"), value, ++ObjectSerial);
                        obj.Kind = kind;
                        if (kind == ItemKind.Charm)
                        {
                            if (charmEffect == "life") obj.CharmEffect = CharmEffectKind.Life;
                            else if (charmEffect == "slow") obj.CharmEffect = CharmEffectKind.Slow;
                            else obj.CharmEffect = CharmEffectKind.Barrier;
                        }
                        obj.SweepId = p.IsSweep ? CurrentSweepId : 0;
                        obj.TrailId = !p.IsSweep && p.Count > 1 ? CurrentTrailId : 0;
                        spawn(obj);
                        PushedInCeremony++;
                        NextPushAt = now + p.StaggerSeconds;
                    }
                    if (PushedInCeremony >= p.Count)
                        EndCeremony(now, derangement, tempo);
                    break;
                }
            }
        }

        void EndCeremony(double now, double derangement, TempoDirector tempo)
        {
            Phase = WavePhase.Cooldown;
            Telegraph = null;
            Pending = null;
            CeremonyIndex++;
            double rate = Fp.Max(_cfg.D("spawn.minRatePerSec"),
                _cfg.D("spawn.baseRatePerSec") * Vec3.Lerpf(1, 1.6, derangement));
            double gap = 1 / rate;
            double warmT = Vec3.Clamp(now / _cfg.D("spawn.warmupDecaySeconds"), 0, 1);
            gap *= Vec3.Lerpf(_cfg.D("spawn.warmupGapBoost"), 1.0, warmT);
            gap *= 0.85 + 0.3 * Rng.Hash01(_seed, CeremonyIndex * 31 + 3);
            double catIntensity = -1, tempoMul = 1;
            if (tempo != null)
            {
                catIntensity = tempo.CatIntensity;
                tempoMul = tempo.GapMultiplier();
                gap *= tempoMul;
            }
            NextSpawnAt = now + gap;
            var payload = new TelemetryPayload()
                .Set("index", CeremonyIndex)
                .Set("nextGap", gap);
            if (tempo != null)
            {
                payload.Set("catIntensity", Fp.RoundNonNeg(catIntensity * 1000) / 1000);
                payload.Set("tempoMul", Fp.RoundNonNeg(tempoMul * 1000) / 1000);
            }
            _telemetry.Emit(now, "cat.ceremony_end", payload);
        }

        double TelegraphSeconds(double derangement)
        {
            double readableWaveBonus = Vec3.Lerpf(
                _cfg.D("telegraph.earlyWaveBonus"),
                _cfg.D("telegraph.lateWaveBonus"),
                derangement);
            double seconds = _cfg.D("telegraph.base") + readableWaveBonus
                - derangement * _cfg.D("telegraph.derangementCut");
            return Vec3.Clamp(seconds, _cfg.D("telegraph.min"), _cfg.D("telegraph.max"));
        }

        double RollSpawnX()
        {
            double halfW = _cfg.D("world.halfWidth") - 0.3;
            return -halfW + 2 * halfW * Rng.Hash01(_seed, CeremonyIndex * 31 + 1);
        }
    }
}
