using NetNinja.Core.Telemetry;

namespace NetNinja.Core
{
    public enum ActiveCharmEffect
    {
        None,
        Barrier,
        Slow,
    }

    public sealed class CharmEffects
    {
        readonly CoreConfig _cfg;
        readonly TelemetrySink _telemetry;

        public ActiveCharmEffect Active = ActiveCharmEffect.None;
        public int BarrierCracksLeft;
        public double SlowUntil = -1;

        public CharmEffects(CoreConfig cfg, TelemetrySink telemetry)
        {
            _cfg = cfg;
            _telemetry = telemetry;
        }

        public void Activate(string effect, double now)
        {
            if (Active != ActiveCharmEffect.None)
            {
                _telemetry.Emit(now, "charm.effectEnd",
                    new Contracts.TelemetryPayload().Set("effect", Active == ActiveCharmEffect.Barrier ? "barrier" : "slow").Set("reason", "replaced"));
            }
            if (effect == "barrier")
            {
                Active = ActiveCharmEffect.Barrier;
                BarrierCracksLeft = _cfg.I("charm.barrier.saves");
                SlowUntil = -1;
            }
            else
            {
                Active = ActiveCharmEffect.Slow;
                SlowUntil = now + _cfg.D("charm.slow.durationSec");
                BarrierCracksLeft = 0;
            }
        }

        public void Tick(double now)
        {
            if (Active == ActiveCharmEffect.Slow && now >= SlowUntil)
            {
                _telemetry.Emit(now, "charm.effectEnd",
                    new Contracts.TelemetryPayload().Set("effect", "slow").Set("reason", "expired"));
                Active = ActiveCharmEffect.None;
                SlowUntil = -1;
            }
        }

        public int ConsumeBarrierCrack(double now, double x)
        {
            BarrierCracksLeft = BarrierCracksLeft - 1;
            if (BarrierCracksLeft < 0) BarrierCracksLeft = 0;
            int left = BarrierCracksLeft;
            _telemetry.Emit(now, "charm.barrierSave",
                new Contracts.TelemetryPayload().Set("x", x).Set("cracksLeft", left));
            if (left <= 0)
            {
                _telemetry.Emit(now, "charm.effectEnd",
                    new Contracts.TelemetryPayload().Set("effect", "barrier").Set("reason", "spent"));
                Active = ActiveCharmEffect.None;
            }
            return left;
        }

        public bool BarrierActive => Active == ActiveCharmEffect.Barrier && BarrierCracksLeft > 0;
        public bool SlowActive => Active == ActiveCharmEffect.Slow;
    }
}
