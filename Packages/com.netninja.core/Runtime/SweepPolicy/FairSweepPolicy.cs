using NetNinja.Contracts;

namespace NetNinja.Core.SweepPolicy
{
    public sealed class FairSweepPolicy : ISweepPolicy
    {
        public string Name => "fair";
        bool _armed;

        public SweepDecision Decide(in SweepContext ctx, CoreConfigView cfgView)
        {
            var cfg = (CoreConfig)cfgView;
            if (!_armed && SweepHelpers.RollWantsSweep(ctx, cfg)) _armed = true;
            if (!_armed) return SweepHelpers.Single(ctx, cfg);
            int count = SweepHelpers.RolledCount(ctx, cfg);
            if (count > ctx.FreeSlots)
            {
                return new SweepDecision
                {
                    Count = 1,
                    Xs = new[] { ctx.NextSpawnX },
                    Direction = 1,
                    StaggerSeconds = 0,
                    IsSweep = false,
                };
            }
            _armed = false;
            return SweepHelpers.ComposeSweep(ctx, cfg, count);
        }
    }
}
