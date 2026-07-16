// Substepped mouth-plane sweep. Port of net-lab packages/core/catchSweep.ts.
namespace NetNinja.Core
{
    public static class CatchSweep
    {
        public const int CatchSweepSubsteps = 8;
        public const int CatchSweepSubstepsMax = 48;
        const double PlaneEps = 0.02;
        const int CrossRefineIters = 10;

        public struct MouthCrossHit
        {
            public double U, Lat, ItemX, ItemY, NetX, NetY;
        }

        public struct SweepPose
        {
            public double PrevItemX, PrevItemY, ItemX, ItemY;
            public double PrevFacingX, PrevFacingY, FacingX, FacingY;
            public double PrevNetX, PrevNetY, NetX, NetY;
        }

        struct Sample
        {
            public double U, ItemX, ItemY, NetX, NetY, S, Lat;
        }

        static double ItemYAt(double u, in SweepPose pose, double dt, double gravity)
        {
            double dy = pose.ItemY - pose.PrevItemY;
            if (dt <= 0 || gravity == 0) return pose.PrevItemY + dy * u;
            double velY0 = dy / dt - gravity * dt;
            double t = u * dt;
            return pose.PrevItemY + velY0 * t + 0.5 * gravity * t * t;
        }

        static Sample SampleAt(double u, in SweepPose pose, double dt, double gravity)
        {
            double itemX = pose.PrevItemX + (pose.ItemX - pose.PrevItemX) * u;
            double itemY = ItemYAt(u, pose, dt, gravity);
            double fx = pose.PrevFacingX + (pose.FacingX - pose.PrevFacingX) * u;
            double fy = pose.PrevFacingY + (pose.FacingY - pose.PrevFacingY) * u;
            double fl = Fp.Sqrt(fx * fx + fy * fy);
            if (fl == 0) fl = 1;
            double nxc = fx / fl, nyc = fy / fl;
            double netX = pose.PrevNetX + (pose.NetX - pose.PrevNetX) * u;
            double netY = pose.PrevNetY + (pose.NetY - pose.PrevNetY) * u;
            double rx = itemX - netX, ry = itemY - netY;
            double s = rx * nxc + ry * nyc;
            double latSq = rx * rx + ry * ry - s * s;
            double lat = Fp.Sqrt(latSq > 0 ? latSq : 0);
            return new Sample { U = u, ItemX = itemX, ItemY = itemY, NetX = netX, NetY = netY, S = s, Lat = lat };
        }

        static int SubstepsForTick(CoreConfig cfg, in SweepPose pose, double itemRadius)
        {
            double itemDisp = Fp.Hypot2(pose.ItemX - pose.PrevItemX, pose.ItemY - pose.PrevItemY);
            double netDisp = Fp.Hypot2(pose.NetX - pose.PrevNetX, pose.NetY - pose.PrevNetY);
            double travel = Fp.Max(itemDisp, netDisp);
            double cell = Fp.Max(cfg.D("net.catch.mouthRadius") * 0.18, Fp.Max(itemRadius * 0.35, 0.06));
            int n = (int)Fp.Ceiling(travel / cell);
            if (n < CatchSweepSubsteps) n = CatchSweepSubsteps;
            if (n > CatchSweepSubstepsMax) n = CatchSweepSubstepsMax;
            return n;
        }

        static Sample RefineCrossU(in SweepPose pose, double dt, double gravity,
            double uLo, double sLo, double uHi, double sHi)
        {
            double lo = uLo, hi = uHi;
            double sAtLo = sLo;
            for (int i = 0; i < CrossRefineIters; i++)
            {
                double mid = (lo + hi) * 0.5;
                double sm = SampleAt(mid, pose, dt, gravity).S;
                if (sAtLo > 0 && sm > 0)
                {
                    lo = mid;
                    sAtLo = sm;
                }
                else hi = mid;
            }
            return SampleAt(hi, pose, dt, gravity);
        }

        public static bool FindMouthPlaneCross(CoreConfig cfg, in SweepPose pose, double itemRadius,
            double dt, double gravity, out MouthCrossHit hit)
        {
            hit = default;
            double mouthR = cfg.D("net.catch.mouthRadius");
            int substeps = SubstepsForTick(cfg, pose, itemRadius);
            var prev = SampleAt(0, pose, dt, gravity);
            for (int i = 1; i <= substeps; i++)
            {
                double u = (double)i / substeps;
                var cur = SampleAt(u, pose, dt, gravity);
                bool crossed = prev.S > -PlaneEps && cur.S <= PlaneEps && prev.S > cur.S;
                if (crossed)
                {
                    var refined = RefineCrossU(pose, dt, gravity, prev.U, prev.S, cur.U, cur.S);
                    if (refined.Lat <= mouthR + itemRadius)
                    {
                        hit = new MouthCrossHit
                        {
                            U = refined.U,
                            Lat = refined.Lat,
                            ItemX = refined.ItemX,
                            ItemY = refined.ItemY,
                            NetX = refined.NetX,
                            NetY = refined.NetY,
                        };
                        return true;
                    }
                }
                prev = cur;
            }
            return false;
        }
    }
}
