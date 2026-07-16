// Intent+Motor plant. Port of net-lab src/adapters/intentMotor.ts.
// Provisional Log/Log2/Cos via Fp (ADR-0008 open question for golden bit-parity).
using NetNinja.Contracts;

namespace NetNinja.Core.Personas
{
    public sealed class MotorParams
    {
        public double Dfloor;
        public double Djitter;
        public double FittsA;
        public double FittsB;
        public double SdnK;
        public double LeadWu;
        public double MisorderRate;
        public double CorrectionRate;
        public double PrimaryUndershoot;
        public double CorrectionGain;
        public double PressureCoupling;
        public int MaxCorrections;
        public double WaveLookAhead;
        public bool WaveTrainSweep;
    }

    enum Verb
    {
        Pour, WaveRide, Catch, Juggle, PourEarly, Reposition, Hold
    }

    struct Decision
    {
        public Verb Verb;
        public Vec3 Goal;
        public int FocusId;
    }

    public sealed class IntentMotorDriver
    {
        readonly MotorParams _motor;
        readonly RngStream _noiseRng;
        readonly RngStream _reactRng;
        readonly RngStream _corrRng;

        Verb _verb = Verb.Hold;
        int _focusId;
        Verb? _pendingVerb;
        int _pendingFocus;
        double _pendingAt;
        Vec3 _pendingGoal = new Vec3(0, -1.15, 0);

        Vec3 _goal = new Vec3(0, -1.15, 0);
        Vec3 _trueGoal = new Vec3(0, -1.15, 0);
        double[] _cx = { 0, 0, 0, 0, 0, 0 };
        double[] _cy = { -1.15, 0, 0, 0, 0, 0 };
        double _segT = 0.4;
        double _segT0;
        Vec3 _planned = new Vec3(0, -1.15, 0);
        int _correctionsDone;
        bool _corrChecked;
        string _pourStage = "approach";
        double? _arcKey;
        double[] _arcXs = System.Array.Empty<double>();
        int _arcDir = 1;

        public IntentMotorDriver(MotorParams motor, int seed)
        {
            _motor = motor;
            _noiseRng = new RngStream(seed, 202);
            _reactRng = new RngStream(seed, 203);
            _corrRng = new RngStream(seed, 204);
        }

        bool CorrectionsEnabled()
            => _motor.CorrectionRate > 0 || _motor.PrimaryUndershoot > 0;

        public void Drive(Sim sim, double dt)
        {
            var want = DecideIntent(sim);
            bool isSwitch = want.Verb != _verb;
            if (!isSwitch && want.FocusId != _focusId)
            {
                _focusId = want.FocusId;
                Plan(sim, want.Goal);
            }
            if (isSwitch)
            {
                bool samePending = _pendingVerb == want.Verb && _pendingFocus == want.FocusId;
                if (!samePending)
                {
                    _pendingVerb = want.Verb;
                    _pendingFocus = want.FocusId;
                    _pendingGoal.Copy(want.Goal);
                    _pendingAt = sim.Time + _motor.Dfloor + _reactRng.Range(0, _motor.Djitter);
                }
                else _pendingGoal.Copy(want.Goal);

                if (_pendingVerb.HasValue && sim.Time >= _pendingAt)
                {
                    _verb = _pendingVerb.Value;
                    _focusId = _pendingFocus;
                    _pendingVerb = null;
                    Plan(sim, _pendingGoal);
                }
            }
            else
            {
                _pendingVerb = null;
                double moved = Vec3.Distance(want.Goal, _trueGoal);
                if (moved > 0.15) TrackGoal(sim, want.Goal);
            }

            MaybeCorrect(sim);

            double t = Vec3.Clamp(sim.Time - _segT0, 0, _segT);
            _planned.Set(Poly(_cx, t), Poly(_cy, t), 0);

            double outX = _planned.X, outY = _planned.Y;
            if (_motor.SdnK > 0)
            {
                double vx = Dpoly(_cx, t), vy = Dpoly(_cy, t);
                double stepLen = Fp.Sqrt(vx * vx + vy * vy) / 60;
                double sigma = _motor.SdnK * stepLen;
                if (sigma > 0)
                {
                    outX += Gauss() * sigma;
                    outY += Gauss() * sigma;
                }
            }

            var cfg = sim.Cfg;
            outX = Vec3.Clamp(outX, -cfg.D("world.halfWidth"), cfg.D("world.halfWidth"));
            outY = Vec3.Clamp(outY, cfg.D("world.floorY") + 0.2, cfg.D("net.catch.maxY") + 0.4);
            sim.Net.Target.Set(outX, outY, 0);
        }

        double Gauss()
        {
            double u1 = Fp.Max(1e-9, _noiseRng.Next());
            double u2 = _noiseRng.Next();
            return Fp.Sqrt(-2 * Fp.Log(u1)) * Fp.Cos(2 * Fp.Pi * u2);
        }

        double GaussCorr()
        {
            double u1 = Fp.Max(1e-9, _corrRng.Next());
            double u2 = _corrRng.Next();
            return Fp.Sqrt(-2 * Fp.Log(u1)) * Fp.Cos(2 * Fp.Pi * u2);
        }

        static double Poly(double[] c, double t)
            => c[0] + t * (c[1] + t * (c[2] + t * (c[3] + t * (c[4] + t * c[5]))));

        static double Dpoly(double[] c, double t)
            => c[1] + t * (2 * c[2] + t * (3 * c[3] + t * (4 * c[4] + t * 5 * c[5])));

        static double Ddpoly(double[] c, double t)
            => 2 * c[2] + t * (6 * c[3] + t * (12 * c[4] + t * 20 * c[5]));

        bool IsStroking()
            => _verb == Verb.Catch || _verb == Verb.Juggle
               || _verb == Verb.WaveRide || (_verb == Verb.Pour && _pourStage == "dive");

        double SegmentTau(Sim sim)
            => _segT <= 1e-6 ? 1 : (sim.Time - _segT0) / _segT;

        void TrackGoal(Sim sim, Vec3 g)
        {
            if (!CorrectionsEnabled()) { Plan(sim, g); return; }
            _trueGoal.Copy(g);
            double couple = _motor.PressureCoupling;
            double earlyTau = Fp.Max(0.45, 0.72 - 0.22 * couple * sim.Derangement);
            int maxC = _motor.MaxCorrections;
            double tau = SegmentTau(sim);
            double rate = _motor.CorrectionRate;
            double effRate = Fp.Min(1, rate * (1 + couple * sim.Derangement));
            if (tau >= earlyTau && _correctionsDone < maxC && _corrRng.Next() < effRate)
            {
                _correctionsDone++;
                double tNow = Vec3.Clamp(sim.Time - _segT0, 0, _segT);
                double px = Poly(_cx, tNow), py = Poly(_cy, tNow);
                PlanSegment(sim, CorrectionTarget(px, py), true, true);
                return;
            }
            ReplanPrimary(sim, g);
        }

        void MaybeCorrect(Sim sim)
        {
            if (!CorrectionsEnabled()) return;
            if (_correctionsDone >= _motor.MaxCorrections) return;
            if (_corrChecked) return;
            if (_segT <= 1e-6) return;
            double couple = _motor.PressureCoupling;
            double earlyTau = Fp.Max(0.55, 0.78 - 0.2 * couple * sim.Derangement);
            if (SegmentTau(sim) < earlyTau) return;
            _corrChecked = true;
            double tNow = Vec3.Clamp(sim.Time - _segT0, 0, _segT);
            double px = Poly(_cx, tNow), py = Poly(_cy, tNow);
            double res = Fp.Hypot2(_trueGoal.X - px, _trueGoal.Y - py);
            var cfg = sim.Cfg;
            double W = 2 * cfg.D("net.catch.mouthRadius") + cfg.D("world.itemRadius");
            double tol = Fp.Max(0.04, 0.15 * W);
            if (res <= tol) return;
            double rate = _motor.CorrectionRate;
            double effRate = Fp.Min(1, rate * (1 + couple * sim.Derangement));
            if (_corrRng.Next() >= effRate) return;
            _correctionsDone++;
            PlanSegment(sim, CorrectionTarget(px, py), true, true);
        }

        (double x, double y) PrimaryTarget(double p0x, double p0y, Vec3 g)
        {
            double under = _motor.PrimaryUndershoot;
            if (IsStroking()) under *= 0.55;
            if (under <= 0 && !CorrectionsEnabled()) return (g.X, g.Y);
            double frac = Vec3.Clamp(1 - under, 0.05, 1);
            double dx = g.X - p0x, dy = g.Y - p0y;
            double A = Fp.Hypot2(dx, dy);
            double tx = p0x + dx * frac;
            double ty = p0y + dy * frac;
            double gain = _motor.CorrectionGain;
            if (gain == 0) gain = 0.5;
            if (A > 1e-6)
            {
                double ux = dx / A, uy = dy / A;
                double latScale = IsStroking() ? 0.04 : 0.10;
                double along = 0.04 * A * Fp.Max(0.2, gain) * GaussCorr();
                double lat = latScale * A * Fp.Max(0.2, gain) * GaussCorr();
                tx += ux * along - uy * lat;
                ty += uy * along + ux * lat;
            }
            return (tx, ty);
        }

        (double x, double y) CorrectionTarget(double endX, double endY)
        {
            double gain = _motor.CorrectionGain;
            if (gain == 0) gain = 1;
            if (IsStroking()) gain = Fp.Min(gain, 1.0);
            double rx = _trueGoal.X - endX;
            double ry = _trueGoal.Y - endY;
            double res = Fp.Hypot2(rx, ry);
            if (res < 1e-6) return (_trueGoal.X, _trueGoal.Y);
            double ux = rx / res, uy = ry / res;
            double latScale = IsStroking() ? 0.06 : 0.18;
            double along = 0.08 * res * Fp.Max(0.2, gain) * GaussCorr();
            double lat = latScale * res * Fp.Max(0.2, gain) * GaussCorr();
            return (endX + gain * rx + ux * along - uy * lat,
                    endY + gain * ry + uy * along + ux * lat);
        }

        void Plan(Sim sim, Vec3 g)
        {
            _trueGoal.Copy(g);
            _correctionsDone = 0;
            ReplanPrimary(sim, g);
        }

        void ReplanPrimary(Sim sim, Vec3 g)
        {
            _trueGoal.Copy(g);
            double tNow = Vec3.Clamp(sim.Time - _segT0, 0, _segT);
            double p0x = Poly(_cx, tNow), p0y = Poly(_cy, tNow);
            var target = CorrectionsEnabled() ? PrimaryTarget(p0x, p0y, g) : (g.X, g.Y);
            PlanSegment(sim, target, false, false);
        }

        void PlanSegment(Sim sim, (double x, double y) target, bool isCorrection, bool preserveVel)
        {
            double tNow = Vec3.Clamp(sim.Time - _segT0, 0, _segT);
            double p0x = Poly(_cx, tNow), p0y = Poly(_cy, tNow);
            bool finished = tNow >= _segT - 1e-9;
            double v0x = 0, v0y = 0, a0x = 0, a0y = 0;
            if (!finished && !isCorrection)
            {
                v0x = Dpoly(_cx, tNow);
                v0y = Dpoly(_cy, tNow);
                a0x = Ddpoly(_cx, tNow);
                a0y = Ddpoly(_cy, tNow);
            }
            else if (!finished && isCorrection && preserveVel)
            {
                double cvx = Dpoly(_cx, tNow), cvy = Dpoly(_cy, tNow);
                double rx = target.x - p0x, ry = target.y - p0y;
                double rlen = Fp.Hypot2(rx, ry);
                if (rlen == 0) rlen = 1;
                const double dip = 0.12;
                v0x = cvx * dip;
                v0y = cvy * dip;
                const double floor = 0.50;
                if (Fp.Hypot2(v0x, v0y) < floor)
                {
                    v0x = (rx / rlen) * floor;
                    v0y = (ry / rlen) * floor;
                }
                a0x = 0;
                a0y = 0;
            }
            _planned.Set(p0x, p0y, 0);
            _goal.Set(target.x, target.y, 0);
            double A = Vec3.Distance(_planned, _goal);
            var cfg = sim.Cfg;
            double W = 2 * cfg.D("net.catch.mouthRadius") + cfg.D("world.itemRadius");
            double ID = Fp.Log2(A / W + 1);
            double T = _motor.FittsA + _motor.FittsB * ID;
            if (isCorrection)
            {
                T *= 0.45;
                double couple = _motor.PressureCoupling;
                T /= 1 + 1.1 * couple * sim.Derangement;
            }
            bool stroking = _verb == Verb.Catch || _verb == Verb.Juggle
                || _verb == Verb.WaveRide || (_verb == Verb.Pour && _pourStage == "dive");
            if (stroking && A > 1e-4)
            {
                double minPeak = cfg.D("net.catch.minSpeed") * 1.6;
                double maxT = (1.875 * A) / minPeak;
                if (!isCorrection && CorrectionsEnabled()) maxT *= 1.55;
                if (T > maxT) T = maxT;
            }
            T = Fp.Max(0.05, T);
            _cx = QuinticBvp(p0x, v0x, a0x, target.x, T);
            _cy = QuinticBvp(p0y, v0y, a0y, target.y, T);
            _segT = T;
            _segT0 = sim.Time;
            _corrChecked = false;
        }

        static double[] QuinticBvp(double p0, double v0, double a0, double g, double T)
        {
            double T2 = T * T, T3 = T2 * T, T4 = T3 * T, T5 = T4 * T;
            double d = g - p0;
            double c3 = (20 * d - (8 * 0 + 12 * v0) * T - (3 * a0 - 0) * T2) / (2 * T3);
            double c4 = (-30 * d + (14 * 0 + 16 * v0) * T + (3 * a0 - 2 * 0) * T2) / (2 * T4);
            double c5 = (12 * d - 6 * (0 + v0) * T + (0 - a0) * T2) / (2 * T5);
            return new[] { p0, v0, a0 / 2, c3, c4, c5 };
        }

        Decision DecideIntent(Sim sim)
        {
            var cfg = sim.Cfg;
            if (sim.Net.IsFull) return PourDecision(sim);
            _pourStage = "approach";

            var tel = sim.Cat.Telegraph;
            if (tel != null && tel.Xs.Length >= 2 && (_arcKey == null || _arcKey.Value != tel.EndsAt))
            {
                _arcKey = tel.EndsAt;
                _arcXs = (double[])tel.Xs.Clone();
                _arcDir = tel.Direction;
            }
            if (_arcKey != null)
            {
                var d = WaveRideDecision(sim);
                if (d.HasValue) return d.Value;
                _arcKey = null;
                _arcXs = System.Array.Empty<double>();
            }

            var fresh = new System.Collections.Generic.List<Decision>();
            var bounced = new System.Collections.Generic.List<Decision>();
            for (int i = 0; i < sim.Objects.Count; i++)
            {
                var o = sim.Objects[i];
                if (o.State != FallState.Falling || o.Banked) continue;
                var cd = CatchDecision(sim, o);
                if (cd == null) continue;
                if (Fp.Abs(o.VelX) > 0.05) bounced.Add(cd.Value);
                else fresh.Add(cd.Value);
            }
            if (fresh.Count > 0) return PickUrgent(fresh);
            if (bounced.Count > 0)
            {
                var d = PickUrgent(bounced);
                d.Verb = Verb.Juggle;
                return d;
            }

            bool anyFalling = false;
            for (int i = 0; i < sim.Objects.Count; i++)
            {
                var o = sim.Objects[i];
                if (o.State == FallState.Falling && !o.Banked) { anyFalling = true; break; }
            }
            if (sim.Net.Held.Count > 0 && !anyFalling && tel == null)
            {
                return new Decision
                {
                    Verb = Verb.PourEarly,
                    Goal = new Vec3(cfg.D("world.binX"), cfg.D("world.binY") + 0.8, 0),
                    FocusId = 0,
                };
            }

            if (tel != null && tel.Xs.Length >= 1)
                return new Decision { Verb = Verb.Reposition, Goal = new Vec3(tel.Xs[0], 0.8, 0), FocusId = 0 };
            if (sim.Cat.Phase == WavePhase.Walk || sim.Cat.Phase == WavePhase.Anticipate)
                return new Decision { Verb = Verb.Reposition, Goal = new Vec3(sim.Cat.CatX, 0.78, 0), FocusId = 0 };

            return new Decision { Verb = Verb.Hold, Goal = new Vec3(0, -1.15, 0), FocusId = 0 };
        }

        Decision PourDecision(Sim sim)
        {
            var cfg = sim.Cfg;
            double bx = cfg.D("world.binX"), by = cfg.D("world.binY");
            double capX = cfg.D("world.binRadius") * cfg.D("net.pour.xCapture");
            bool overX = Fp.Abs(sim.Net.Pos.X - bx) <= capX;
            double hAbove = sim.Net.Pos.Y - by;
            if (!overX || hAbove > cfg.D("net.pour.heightAbove") || hAbove < 0.25)
            {
                _pourStage = "approach";
                return new Decision { Verb = Verb.Pour, Goal = new Vec3(bx, by + 0.8, 0), FocusId = 0 };
            }
            _pourStage = "dive";
            return new Decision { Verb = Verb.Pour, Goal = new Vec3(bx, by - 0.95, 0), FocusId = 1 };
        }

        Decision? WaveRideDecision(Sim sim)
        {
            double startX = _arcDir > 0 ? _arcXs[0] : _arcXs[_arcXs.Length - 1];
            var sweep = new System.Collections.Generic.List<FallingObject>();
            for (int i = 0; i < sim.Objects.Count; i++)
            {
                var o = sim.Objects[i];
                if (o.State == FallState.Falling && !o.Banked && (o.SweepId != 0 || o.TrailId != 0))
                    sweep.Add(o);
            }
            FallingObject front = sweep.Count > 0 ? sweep[0] : null;
            for (int i = 1; i < sweep.Count; i++)
                if (sweep[i].Pos.Y < front.Pos.Y) front = sweep[i];

            if (front == null)
            {
                bool ceremony = sim.Cat.Phase == WavePhase.Telegraph || sim.Cat.Phase == WavePhase.Pushing
                    || sim.Time < _arcKey.Value;
                if (!ceremony) return null;
                bool stray = false;
                for (int i = 0; i < sim.Objects.Count; i++)
                {
                    var o = sim.Objects[i];
                    if (o.State == FallState.Falling && !o.Banked && CatchDecision(sim, o) != null)
                    { stray = true; break; }
                }
                if (stray) return null;
                return new Decision { Verb = Verb.WaveRide, Goal = new Vec3(startX, 0.76, 0), FocusId = 0 };
            }
            if (front.Pos.Y < -1.8) return null;
            if (front.Pos.Y < 0.9 && sweep.Count > 1)
            {
                FallingObject next = null;
                for (int i = 0; i < sweep.Count; i++)
                {
                    var o = sweep[i];
                    if (o == front) continue;
                    if (next == null || o.Pos.Y < next.Pos.Y) next = o;
                }
                if (next != null) front = next;
            }
            double slotX = front.Pos.X, minD = double.PositiveInfinity;
            for (int i = 0; i < _arcXs.Length; i++)
            {
                double d = Fp.Abs(_arcXs[i] - front.Pos.X);
                if (d < minD) { minD = d; slotX = _arcXs[i]; }
            }
            if (minD > 0.5) slotX = front.Pos.X;
            bool isSweep = front.SweepId != 0;
            double stageY = isSweep ? 0.45 : 0.78;
            double tyLo = isSweep ? 0.0 : 0.2;
            double tyHi = isSweep ? 0.5 : 0.9;
            if (front.Pos.Y > 1.6)
                return new Decision { Verb = Verb.WaveRide, Goal = new Vec3(slotX, stageY, 0), FocusId = front.Id };
            if (_motor.WaveTrainSweep && sweep.Count > 1 && front.Pos.Y <= 1.0)
            {
                var lastItem = front;
                for (int i = 0; i < sweep.Count; i++)
                {
                    var o = sweep[i];
                    if ((o.Pos.X - lastItem.Pos.X) * _arcDir > 0) lastItem = o;
                }
                if (lastItem != front && Fp.Abs(lastItem.Pos.X - front.Pos.X) > 0.3)
                {
                    double ty = Vec3.Clamp(front.Pos.Y, tyLo, tyHi);
                    var g2 = StrokeThrough(sim, lastItem.Pos.X, ty, -0.4);
                    return new Decision { Verb = Verb.WaveRide, Goal = g2, FocusId = front.Id };
                }
            }
            double ty2 = Vec3.Clamp(front.Pos.Y, tyLo, tyHi);
            double look = _motor.WaveLookAhead;
            if (look > 0)
            {
                var probe = StrokeThrough(sim, slotX, ty2, -0.4);
                double W = 2 * sim.Cfg.D("net.catch.mouthRadius") + sim.Cfg.D("world.itemRadius");
                double strikeMT = _motor.FittsA + _motor.FittsB * Fp.Log2(Vec3.Distance(_planned, probe) / W + 1);
                double tt = strikeMT * look;
                double gAcc = sim.Cfg.D("world.fallGravity");
                ty2 = Vec3.Clamp(front.Pos.Y + front.VelY * tt + 0.5 * gAcc * tt * tt, tyLo, tyHi);
            }
            var g = StrokeThrough(sim, slotX, ty2, -0.4);
            return new Decision { Verb = Verb.WaveRide, Goal = g, FocusId = front.Id };
        }

        double FallTimeTo(Sim sim, FallingObject o, double yTo)
        {
            double g = sim.Cfg.D("world.fallGravity");
            double disc = o.VelY * o.VelY + 2 * g * (yTo - o.Pos.Y);
            if (disc < 0) return 0;
            return (-o.VelY - Fp.Sqrt(disc)) / g;
        }

        Decision? CatchDecision(Sim sim, FallingObject o)
        {
            var cfg = sim.Cfg;
            if (o.Pos.Y > cfg.D("net.catch.maxY") + 0.6) return null;
            if (o.Pos.Y < -1.6) return null;
            double tLeft = Fp.Max(0, FallTimeTo(sim, o, -1.6));
            double W = 2 * cfg.D("net.catch.mouthRadius") + cfg.D("world.itemRadius");
            const double yCross = 0.35;
            if (o.Pos.Y <= yCross + 0.15)
            {
                var goal = StrokeThrough(sim, o.Pos.X, Fp.Max(o.Pos.Y, -0.4), -0.8);
                double A = Vec3.Distance(sim.Net.Pos, goal);
                double MT = _motor.FittsA + _motor.FittsB * Fp.Log2(A / W + 1);
                if (MT > Fp.Max(0.15, tLeft)) return null;
                return new Decision { Verb = Verb.Catch, Goal = goal, FocusId = o.Id };
            }
            double tCross = FallTimeTo(sim, o, yCross);
            double px = Vec3.Clamp(o.Pos.X + o.VelX * tCross, -cfg.D("world.halfWidth"), cfg.D("world.halfWidth"));
            var strike = StrokeThrough(sim, px, yCross, -0.8);
            double A2 = Vec3.Distance(_planned, strike);
            double strikeMT = _motor.FittsA + _motor.FittsB * Fp.Log2(A2 / W + 1);
            if (tCross > strikeMT + 0.05)
            {
                int side = _planned.X >= px ? 1 : -1;
                double sx = Vec3.Clamp(px + side * 1.1, -cfg.D("world.halfWidth"), cfg.D("world.halfWidth"));
                double stageDist = Vec3.Distance(_planned, new Vec3(sx, yCross, 0));
                double stageMT = _motor.FittsA + _motor.FittsB * Fp.Log2(stageDist / W + 1);
                if (stageMT < tCross - strikeMT)
                    return new Decision { Verb = Verb.Catch, Goal = new Vec3(sx, yCross, 0), FocusId = o.Id };
            }
            double MT2 = _motor.FittsA + _motor.FittsB * Fp.Log2(Vec3.Distance(sim.Net.Pos, strike) / W + 1);
            if (MT2 > Fp.Max(0.15, tLeft)) return null;
            return new Decision { Verb = Verb.Catch, Goal = strike, FocusId = o.Id };
        }

        Vec3 StrokeThrough(Sim sim, double tx, double ty, double yFloor)
        {
            var cfg = sim.Cfg;
            double nx = _planned.X, ny = _planned.Y;
            double dx = tx - nx, dy = ty - ny;
            double dl = Fp.Sqrt(dx * dx + dy * dy);
            if (dl < 0.12) { dx = 1; dy = -0.38; dl = 1; }
            else { dx /= dl; dy /= dl; }
            double lead = _motor.LeadWu;
            double gy = Vec3.Clamp(ty + dy * lead, yFloor, cfg.D("net.catch.maxY") - 0.05);
            double gx = Vec3.Clamp(tx + dx * lead, -cfg.D("world.halfWidth"), cfg.D("world.halfWidth"));
            if (sim.Net.Held.Count > 0 && dy < 0)
            {
                double bx = cfg.D("world.binX");
                double capX = cfg.D("world.binRadius") * cfg.D("net.pour.xCapture") + 0.15;
                if (Fp.Abs(gx - bx) < capX)
                {
                    gx = Vec3.Clamp(bx + (gx >= bx ? capX : -capX), -cfg.D("world.halfWidth"), cfg.D("world.halfWidth"));
                    gy = Fp.Max(gy, -0.2);
                }
            }
            return new Vec3(gx, gy, 0);
        }

        Decision PickUrgent(System.Collections.Generic.List<Decision> list)
        {
            int bestI = 0;
            for (int i = 1; i < list.Count; i++)
                if (list[i].Goal.Y < list[bestI].Goal.Y) bestI = i;
            if (_motor.MisorderRate > 0 && list.Count > 1 && _reactRng.Next() < _motor.MisorderRate)
                return list[bestI == 0 ? 1 : 0];
            return list[bestI];
        }
    }
}
