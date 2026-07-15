using NetNinja.Contracts;

namespace NetNinja.Core
{
    public sealed class TempoDirector
    {
        readonly CoreConfig _cfg;
        public double CatIntensity;

        public TempoDirector(CoreConfig cfg)
        {
            _cfg = cfg;
            CatIntensity = cfg.D("tempo.baseline");
        }

        void Heat(double amount) => CatIntensity = Vec3.Clamp01(CatIntensity + amount);

        public void OnCatch() => Heat(_cfg.D("tempo.risePerCatch"));
        public void OnWaveCatch() => Heat(_cfg.D("tempo.risePerWave"));
        public void OnTrailClear() => Heat(_cfg.D("tempo.risePerTrail"));
        public void OnDump() => Heat(_cfg.D("tempo.risePerDump"));
        public void OnMiss() => Heat(-_cfg.D("tempo.dropPerMiss"));
        public void OnRimClank() => Heat(-_cfg.D("tempo.rimCool"));

        public void Tick(double dt)
        {
            double bas = _cfg.D("tempo.baseline");
            double step = _cfg.D("tempo.reboundPerSec") * dt;
            if (CatIntensity > bas) CatIntensity = Fp.Max(bas, CatIntensity - step);
            else if (CatIntensity < bas) CatIntensity = Fp.Min(bas, CatIntensity + step);
        }

        public double GapMultiplier()
        {
            double bas = _cfg.D("tempo.baseline");
            double thr = _cfg.D("tempo.compressThreshold");
            double ci = CatIntensity;
            if (ci >= thr)
            {
                double t = thr >= 1 ? 0 : (ci - thr) / (1 - thr);
                return Vec3.Lerpf(1, _cfg.D("tempo.gapCompressMin"), Vec3.Clamp01(t));
            }
            if (ci <= bas)
            {
                double t = bas <= 0 ? 0 : (bas - ci) / bas;
                return Vec3.Lerpf(1, _cfg.D("tempo.gapEaseMax"), Vec3.Clamp01(t));
            }
            return 1;
        }
    }
}
