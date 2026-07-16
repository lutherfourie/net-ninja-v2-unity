using System.Collections.Generic;
using NetNinja.Contracts;
using NetNinja.Core.Telemetry;

namespace NetNinja.Core
{
    public sealed class SweepTracker
    {
        public int SweepId;
        public double Expected;
        public double Caught;
        public double Missed;
        public double StartedAt;
        public double Deadline;
        public bool Failed;
    }

    public sealed class ScoreManager
    {
        readonly CoreConfig _cfg;
        readonly TelemetrySink _telemetry;

        public double Score;
        public double Combo;
        public int Lives;
        public int Misses;
        public int WaveCatches;
        public double Temper;

        // internal for hasher
        internal readonly Dictionary<int, SweepTracker> Sweeps = new Dictionary<int, SweepTracker>();
        internal readonly Dictionary<int, SweepTracker> Trails = new Dictionary<int, SweepTracker>();

        public ScoreManager(CoreConfig cfg, TelemetrySink telemetry)
        {
            _cfg = cfg;
            _telemetry = telemetry;
            Lives = cfg.I("run.lives");
        }

        public double TemperMultiplier => 1 + Temper * _cfg.D("temper.scoreBonusMax");
        public bool GameOver => Lives <= 0;

        public void Tick(double dt)
            => Temper = Fp.Max(0, Temper - _cfg.D("temper.decayPerSec") * dt);

        public void ExpireTrackers(double now)
        {
            var toFail = new List<int>();
            foreach (var kv in Sweeps)
            {
                var s = kv.Value;
                if (!s.Failed && now > s.Deadline && s.Caught < s.Expected) s.Failed = true;
                if (s.Failed) toFail.Add(kv.Key);
            }
            foreach (var id in toFail) Sweeps.Remove(id);
            toFail.Clear();
            foreach (var kv in Trails)
            {
                var tr = kv.Value;
                if (!tr.Failed && now > tr.Deadline && tr.Caught < tr.Expected) tr.Failed = true;
                if (tr.Failed) toFail.Add(kv.Key);
            }
            foreach (var id in toFail) Trails.Remove(id);
        }

        void ResolveSweepMiss(SweepTracker s, double now)
        {
            if (s.Failed) return;
            if (s.Caught + s.Missed >= s.Expected && s.Caught < s.Expected) s.Failed = true;
            if (now > s.Deadline && s.Caught < s.Expected) s.Failed = true;
        }

        public void CoolFromRim()
            => Temper = Fp.Max(0, Temper - _cfg.D("temper.rimCool"));

        void HeatUp(double amount)
            => Temper = Fp.Min(1, Temper + amount);

        public void TrackSweep(int sweepId, double expected, double stagger, double now)
        {
            double duration = stagger * (expected - 1);
            double fallHeadroom = 2.8;
            Sweeps[sweepId] = new SweepTracker
            {
                SweepId = sweepId,
                Expected = expected,
                Caught = 0,
                Missed = 0,
                StartedAt = now,
                Deadline = now + duration + fallHeadroom + _cfg.D("net.sweep.wavecatchGraceSeconds"),
                Failed = false,
            };
        }

        public void TrackTrail(int trailId, double expected, double now)
        {
            Trails[trailId] = new SweepTracker
            {
                SweepId = trailId,
                Expected = expected,
                Caught = 0,
                Missed = 0,
                StartedAt = now,
                Deadline = now + 6,
                Failed = false,
            };
        }

        public void RegisterCatch(FallingObject item, double now)
        {
            Combo++;
            HeatUp(_cfg.D("temper.perCatch"));
            if (item.TrailId != 0 && Trails.TryGetValue(item.TrailId, out var tr) && !tr.Failed)
            {
                tr.Caught++;
                if (tr.Caught >= tr.Expected && now <= tr.Deadline)
                {
                    HeatUp(_cfg.D("temper.trailBonus"));
                    int bonus = Fp.RoundToIntNonNeg(5 * tr.Expected * TemperMultiplier);
                    Score += bonus;
                    _telemetry.Emit(now, "net.trailclear",
                        new TelemetryPayload().Set("count", tr.Expected).Set("bonus", bonus));
                    Trails.Remove(item.TrailId);
                }
            }
            if (item.SweepId != 0 && Sweeps.TryGetValue(item.SweepId, out var s) && !s.Failed)
            {
                s.Caught++;
                if (s.Caught >= s.Expected && now <= s.Deadline)
                {
                    WaveCatches++;
                    HeatUp(_cfg.D("temper.waveBonus"));
                    int durationMs = Fp.RoundToIntNonNeg((now - s.StartedAt) * 1000);
                    _telemetry.Emit(now, "net.wavecatch",
                        new TelemetryPayload().Set("count", s.Expected).Set("durationMs", durationMs));
                    Score += Fp.RoundToIntNonNeg(25 * s.Expected * TemperMultiplier);
                    Sweeps.Remove(item.SweepId);
                }
            }
        }

        public void RegisterMiss(FallingObject item, double now)
        {
            Misses++;
            Lives--;
            Combo = 0;
            Temper *= 1 - _cfg.D("temper.missCool");
            if (item.SweepId != 0 && Sweeps.TryGetValue(item.SweepId, out var s))
            {
                s.Missed++;
                ResolveSweepMiss(s, now);
            }
            if (item.TrailId != 0 && Trails.TryGetValue(item.TrailId, out var tr))
            {
                tr.Missed++;
                if (tr.Caught + tr.Missed >= tr.Expected && tr.Caught < tr.Expected) tr.Failed = true;
            }
            var payload = new TelemetryPayload()
                .Set("x", item.Pos.X)
                .Set("sweepId", item.SweepId)
                .Set("livesLeft", Lives);
            if (_cfg.B("items.taxonomy.enabled") && item.Kind != ItemKind.Plain)
                payload.Set("kind", item.Kind.ToString().ToLowerInvariant());
            _telemetry.Emit(now, "net.miss", payload);
        }

        public void RegisterDump(List<FallingObject> items, double now, bool flicked, double derangement)
        {
            double bas = 0, rungBonus = 0, heapBonus = 0;
            int heapedCount = 0;
            for (int i = 0; i < items.Count; i++)
            {
                var item = items[i];
                bas += item.Value * (1 + Combo * 0.1);
                rungBonus += item.Value * (RungMult(item.Rungs) - 1);
                if (_cfg.B("net.heap.enabled") && item.Heaped)
                {
                    heapBonus += item.Value * _cfg.D("net.heap.pourGreedMult") * derangement;
                    heapedCount++;
                }
            }
            double countBonus = 1 + _cfg.D("net.dumpCountBonusPct") * items.Count;
            double flickBonus = flicked ? 1 + _cfg.D("net.flick.bonusPct") : 1;
            int gained = Fp.RoundToIntNonNeg((bas * countBonus * flickBonus + rungBonus + heapBonus) * TemperMultiplier);
            Score += gained;
            var payload = new TelemetryPayload()
                .Set("items", items.Count)
                .Set("gained", gained)
                .Set("flicked", flicked)
                .Set("temper", Fp.RoundNonNeg(Temper * 100) / 100);
            if (_cfg.B("juggle.ladder.enabled"))
            {
                int rungs = 0;
                for (int i = 0; i < items.Count; i++) rungs += items[i].Rungs;
                payload.Set("rungs", rungs);
            }
            if (_cfg.B("net.heap.enabled") && heapedCount > 0)
                payload.Set("heaped", heapedCount);
            _telemetry.Emit(now, "score.dump", payload);
        }

        double RungMult(int r)
        {
            if (r <= 0) return 1;
            if (r == 1) return _cfg.D("juggle.mult1");
            if (r == 2) return _cfg.D("juggle.mult2");
            return _cfg.D("juggle.mult3");
        }
    }
}
