using NetNinja.Contracts;

namespace NetNinja.Core.SweepPolicy
{
    public sealed class TriageSweepPolicy : ISweepPolicy
    {
        public string Name => "triage";

        public SweepDecision Decide(in SweepContext ctx, CoreConfigView cfgView)
        {
            var cfg = (CoreConfig)cfgView;
            if (!SweepHelpers.RollWantsSweep(ctx, cfg)) return SweepHelpers.Single(ctx, cfg);
            return SweepHelpers.ComposeSweep(ctx, cfg, SweepHelpers.RolledCount(ctx, cfg));
        }
    }

    public sealed class NoSweepPolicy : ISweepPolicy
    {
        public string Name => "off";

        public SweepDecision Decide(in SweepContext ctx, CoreConfigView cfgView)
            => SweepHelpers.Single(ctx, (CoreConfig)cfgView);
    }
}
