using NetNinja.Contracts;

namespace NetNinja.Core.SweepPolicy
{
    public static class PolicyRegistry
    {
        public static ISweepPolicy Make(string name)
        {
            if (name == "fair") return new FairSweepPolicy();
            if (name == "triage") return new TriageSweepPolicy();
            return new NoSweepPolicy();
        }

        public static int ItemsPerPushNow(CoreConfig cfg, double derangement)
        {
            double v = NetNinja.Contracts.Vec3.Lerpf(
                cfg.D("spawn.itemsPerPush"),
                cfg.D("spawn.itemsPerPushMax"),
                derangement);
            return Fp.RoundToIntNonNeg(v);
        }
    }

    static class SweepHelpers
    {
        public static SweepDecision Single(in SweepContext ctx, CoreConfig cfg)
        {
            int n = PolicyRegistry.ItemsPerPushNow(cfg, ctx.Derangement);
            if (n < 1) n = 1;
            double spread = cfg.D("spawn.pushSpreadUnits");
            double halfW = cfg.D("world.halfWidth") - 0.3;
            int direction = Rng.Hash01(ctx.Seed, ctx.CeremonyIndex * 31 + 19) < 0.5 ? -1 : 1;
            var xs = new double[n];
            for (int i = 0; i < n; i++)
            {
                double t = n == 1 ? 0.5 : (double)i / (n - 1);
                double x = ctx.NextSpawnX + direction * (t - 0.5) * spread;
                if (x > halfW) x = halfW;
                if (x < -halfW) x = -halfW;
                xs[i] = x;
            }
            return new SweepDecision
            {
                Count = n,
                Xs = xs,
                Direction = direction,
                StaggerSeconds = n > 1 ? cfg.D("spawn.pushStaggerSeconds") : 0,
                IsSweep = false,
            };
        }

        public static bool RollWantsSweep(in SweepContext ctx, CoreConfig cfg)
        {
            if (ctx.SimTime - ctx.LastSweepAt < cfg.D("net.sweep.cooldownSeconds")) return false;
            if (ctx.CeremonyIndex == 0) return false;
            double chance = NetNinja.Contracts.Vec3.Lerpf(0, cfg.D("net.sweep.chanceMax"), ctx.Derangement);
            return Rng.Hash01(ctx.Seed, ctx.CeremonyIndex * 31 + 7) < chance;
        }

        public static SweepDecision ComposeSweep(in SweepContext ctx, CoreConfig cfg, int count)
        {
            double dirRoll = Rng.Hash01(ctx.Seed, ctx.CeremonyIndex * 31 + 13);
            int direction = dirRoll < 0.5 ? -1 : 1;
            double span = cfg.D("net.sweep.spanUnits");
            double halfW = cfg.D("world.halfWidth");
            double originRoll = Rng.Hash01(ctx.Seed, ctx.CeremonyIndex * 31 + 17);
            double margin = span / 2 + 0.2;
            double center = -halfW + margin + originRoll * 2 * (halfW - margin);
            var xs = new double[count];
            for (int i = 0; i < count; i++)
            {
                double t = count == 1 ? 0.5 : (double)i / (count - 1);
                xs[i] = center + direction * (t - 0.5) * span;
            }
            return new SweepDecision
            {
                Count = count,
                Xs = xs,
                Direction = direction,
                StaggerSeconds = cfg.D("net.sweep.staggerSeconds"),
                IsSweep = true,
            };
        }

        public static int RolledCount(in SweepContext ctx, CoreConfig cfg)
            => ctx.Derangement >= cfg.D("net.sweep.highDerangement")
                ? cfg.I("net.sweep.countHigh")
                : cfg.I("net.sweep.countBase");
    }
}
