using System.Collections.Generic;
using NetNinja.Contracts;
using NetNinja.Core.Telemetry;

namespace NetNinja.Core
{
    public sealed class NetCatcher
    {
        readonly CoreConfig _cfg;
        readonly TelemetrySink _telemetry;

        public Vec3 Pos = new Vec3(0, -2.5, 0);
        public Vec3 Target = new Vec3(0, -2.5, 0);
        public readonly List<FallingObject> Held = new List<FallingObject>();
        public double VelX, VelY;
        public double FacingX, FacingY = 1;
        internal double PrevX, PrevY = -2.5;
        internal double PrevFacingX, PrevFacingY = 1;
        string _lastPourReject;
        bool _rimBouncedThisCatch;
        public double CatchCarrySec;
        public bool PointerFollow;
        double _smoothVelX, _smoothVelY;
        public double HeapStability = 1;
        public double SimNow;
        public double BrokenUntil;
        public int? DynamicCapacity;

        public NetCatcher(CoreConfig cfg, TelemetrySink telemetry)
        {
            _cfg = cfg;
            _telemetry = telemetry;
        }

        public bool IsCatchCommitted()
        {
            if (!_cfg.B("net.catch.directional")) return true;
            double speed = Fp.Sqrt(VelX * VelX + VelY * VelY);
            return speed >= _cfg.D("net.catch.minSpeed") || CatchCarrySec > 0;
        }

        public int Capacity => DynamicCapacity ?? _cfg.I("net.capacity");
        public int FreeSlots => Capacity - Held.Count;
        public bool IsFull => Held.Count >= Capacity;
        public int HeapCount => Held.Count > Capacity ? Held.Count - Capacity : 0;

        public void Step(double dt)
        {
            double fill = Vec3.Clamp01((double)Held.Count / Capacity);
            double weight = fill * Fp.Sqrt(fill);
            double a0 = PointerFollow ? _cfg.D("net.followAlphaEmptyPointer") : _cfg.D("net.followAlphaEmpty");
            double a1 = PointerFollow ? _cfg.D("net.followAlphaFullPointer") : _cfg.D("net.followAlphaFull");
            double t = Vec3.Lerpf(a0, a1, weight);
            PrevX = Pos.X;
            PrevY = Pos.Y;
            Pos = Vec3.Lerp(Pos, Target, t);
            VelX = (Pos.X - PrevX) / dt;
            VelY = (Pos.Y - PrevY) / dt;
            if (PointerFollow)
            {
                const double k = 0.35;
                _smoothVelX += (VelX - _smoothVelX) * k;
                _smoothVelY += (VelY - _smoothVelY) * k;
            }
            else
            {
                _smoothVelX = VelX;
                _smoothVelY = VelY;
            }
            PrevFacingX = FacingX;
            PrevFacingY = FacingY;
            bool nearBinX = Fp.Abs(Pos.X - _cfg.D("world.binX"))
                <= _cfg.D("world.binRadius") * _cfg.D("net.pour.xCapture");
            double fvx = PointerFollow ? _smoothVelX : VelX;
            double fvy = PointerFollow ? _smoothVelY : VelY;
            double speed = Fp.Sqrt(fvx * fvx + fvy * fvy);
            double facingSpeedMin = Held.Count > 0 && nearBinX ? 0.35 : 1.0;
            if (speed > facingSpeedMin)
            {
                double txf = fvx / speed;
                double tyf = fvy / speed;
                double ease = _cfg.D("net.facingEaseAlpha");
                if (Held.Count > 0 && nearBinX) ease = _cfg.D("net.pour.facingEaseAlpha");
                FacingX += (txf - FacingX) * ease;
                FacingY += (tyf - FacingY) * ease;
                double fl = Fp.Sqrt(FacingX * FacingX + FacingY * FacingY);
                if (fl == 0) fl = 1;
                FacingX /= fl;
                FacingY /= fl;
            }
            else if (_cfg.B("net.facing.weightedSelfRight") && Held.Count > 0 && !nearBinX)
            {
                double heldFill = Vec3.Clamp01((double)Held.Count / Capacity);
                double k = Fp.Min(1, _cfg.D("net.facing.selfRightRate") * heldFill * dt);
                FacingX += (0 - FacingX) * k;
                FacingY += (1 - FacingY) * k;
                double fl = Fp.Sqrt(FacingX * FacingX + FacingY * FacingY);
                if (fl == 0) fl = 1;
                FacingX /= fl;
                FacingY /= fl;
            }
            else if (Held.Count == 0 && _cfg.S("net.restPose.mode") != "hold" && !nearBinX)
            {
                if (_cfg.S("net.restPose.mode") == "level")
                {
                    double k = Fp.Min(1, _cfg.D("net.restPose.rate") * dt);
                    FacingX += (0 - FacingX) * k;
                    FacingY += (1 - FacingY) * k;
                    double fl = Fp.Sqrt(FacingX * FacingX + FacingY * FacingY);
                    if (fl == 0) fl = 1;
                    FacingX /= fl;
                    FacingY /= fl;
                }
            }
            if (_cfg.B("net.facing.clampUp") && !nearBinX && FacingY < 0)
            {
                FacingX = FacingX != 0 ? Fp.Sign(FacingX)
                    : PrevFacingX != 0 ? Fp.Sign(PrevFacingX) : 1;
                FacingY = 0;
            }
            double carrySec = _cfg.D("net.catch.speedCarryMs") / 1000;
            if (!_cfg.B("net.catch.directional") || speed >= _cfg.D("net.catch.minSpeed"))
                CatchCarrySec = carrySec;
            else
                CatchCarrySec = Fp.Max(0, CatchCarrySec - dt);

            if (_cfg.B("net.heap.enabled"))
            {
                if (HeapCount > 0)
                {
                    double hvx = PointerFollow ? _smoothVelX : VelX;
                    double hvy = PointerFollow ? _smoothVelY : VelY;
                    double heapSpeed = Fp.Sqrt(hvx * hvx + hvy * hvy);
                    double excess = Fp.Max(0, heapSpeed - _cfg.D("net.heap.calmSpeed"));
                    if (excess > 0)
                        HeapStability -= excess * HeapCount * _cfg.D("net.heap.drainK") * dt;
                    else
                        HeapStability = Fp.Min(1, HeapStability + _cfg.D("net.heap.regenPerSec") * dt);
                    if (HeapStability <= 0) ShedTop(SimNow);
                }
                else if (HeapStability < 1) HeapStability = 1;
            }
            for (int i = 0; i < Held.Count; i++)
                Held[i].Pos.Copy(Pos);
        }

        void ShedTop(double now)
        {
            if (Held.Count == 0) { HeapStability = 1; return; }
            var item = Held[Held.Count - 1];
            if (!item.Heaped) { HeapStability = 1; return; }
            Held.RemoveAt(Held.Count - 1);
            item.State = FallState.Falling;
            item.Heaped = false;
            item.Pos.Set(Pos.X + FacingX * 0.45, Pos.Y + FacingY * 0.45, 0);
            item.VelX = FacingX * _cfg.D("net.heap.shedUpVel") + VelX * 0.3;
            item.VelY = Fp.Max(1.0, FacingY * _cfg.D("net.heap.shedUpVel") + 0.6);
            item.ShedImmuneUntil = now + _cfg.D("net.heap.shedImmunitySec");
            HeapStability = 1;
            _telemetry.Emit(now, "net.shed", new TelemetryPayload()
                .Set("x", item.Pos.X).Set("y", item.Pos.Y).Set("heapCount", HeapCount));
        }

        public void ApplyMagnet(FallingObject item, double dt)
        {
            if (item.Kind == ItemKind.Knife) return;
            if (IsFull) return;
            if (!IsCatchCommitted()) return;
            double dx = Pos.X - item.Pos.X, dy = Pos.Y - item.Pos.Y;
            double d = Fp.Sqrt(dx * dx + dy * dy);
            double R = _cfg.D("net.magnet.radius");
            if (d >= R || d < 1e-4) return;
            double pull = _cfg.D("net.magnet.strength") * (1 - d / R);
            item.VelX += (dx / d) * pull * dt;
            item.VelY += (dy / d) * pull * dt;
        }

        void WebDeflect(FallingObject item)
        {
            item.Pos.X += FacingX * 0.06;
            item.Pos.Y = Fp.Min(item.Pos.Y + FacingY * 0.06, _cfg.D("net.catch.maxY") - 0.001);
            double rawLift = Fp.Abs(FacingY) * _cfg.D("items.webBounceSpeed")
                + Fp.Max(0, VelY) * _cfg.D("net.rim.velTransfer");
            double headroom = _cfg.D("net.catch.maxY") - 0.1 - item.Pos.Y;
            if (headroom >= _cfg.D("items.webBounceReadBand"))
            {
                double ceilCap = Fp.Sqrt(2 * Fp.Abs(_cfg.D("world.fallGravity")) * headroom);
                item.VelY = Fp.Min(Fp.Max(rawLift, _cfg.D("items.webBounceMinLift")), ceilCap);
            }
            else
                item.VelY = -0.6 * _cfg.D("items.webBounceMinLift");
            item.VelX = FacingX * _cfg.D("items.webBounceSpeed") + VelX * _cfg.D("net.rim.velTransfer");
        }

        bool TryArmRung(FallingObject item, double now)
        {
            bool canRung = item.Rungs < _cfg.I("juggle.rungCap")
                && (now - item.LastRungAt) >= _cfg.D("juggle.rungDebounceSec");
            if (!canRung) return false;
            item.Rungs++;
            item.LastRungAt = now;
            return true;
        }

        void BreakNet(FallingObject knife, double now)
        {
            var spilled = new List<FallingObject>(Held);
            Held.Clear();
            int n = spilled.Count;
            for (int i = 0; i < n; i++)
            {
                var heldItem = spilled[i];
                heldItem.State = FallState.Falling;
                heldItem.Heaped = false;
                heldItem.SweepId = 0;
                heldItem.TrailId = 0;
                double spread = (i - (n - 1) / 2.0) * 0.12;
                heldItem.Pos.Set(Pos.X + spread * 0.3, Pos.Y, 0);
                heldItem.VelX = spread * 0.4 + VelX * 0.3;
                heldItem.VelY = 2.4 + i * 0.15;
            }
            BrokenUntil = now + _cfg.D("net.break.repairSeconds");
            knife.KoUntilGone = true;
            knife.VelX = FacingX * 1.2;
            knife.VelY = -3.2;
            _telemetry.Emit(now, "net.break", new TelemetryPayload()
                .Set("x", Pos.X).Set("y", Pos.Y).Set("spilled", n));
        }

        void TryRimBounce(FallingObject item, double now)
        {
            if (item.KoUntilGone) return;
            if (now < BrokenUntil) return;
            double nx0 = FacingX, ny0 = FacingY;
            double rx = item.Pos.X - Pos.X, ry = item.Pos.Y - Pos.Y;
            double s0 = rx * nx0 + ry * ny0;
            if (s0 < -0.1) return;
            double lx = rx - s0 * nx0, ly = ry - s0 * ny0;
            double l0 = Fp.Sqrt(lx * lx + ly * ly);
            if (l0 == 0) l0 = 1e-6;
            double mouthR = _cfg.D("net.catch.mouthRadius");
            if (l0 < mouthR - item.Radius * 0.35) return;
            double dRim = Fp.Sqrt((l0 - mouthR) * (l0 - mouthR) + s0 * s0);
            if (dRim >= item.Radius + _cfg.D("net.rim.tube")) return;
            double outX = ((l0 - mouthR) * (lx / l0) + s0 * nx0) / (dRim == 0 ? 1e-6 : dRim);
            double outY = ((l0 - mouthR) * (ly / l0) + s0 * ny0) / (dRim == 0 ? 1e-6 : dRim);
            double outLen = Fp.Sqrt(outX * outX + outY * outY);
            if (outLen == 0) outLen = 1e-6;
            if (item.Kind == ItemKind.Knife && !item.KoUntilGone)
            {
                double netSpeed = Fp.Sqrt(VelX * VelX + VelY * VelY);
                if (netSpeed >= _cfg.D("net.parry.minSpeed"))
                {
                    item.KoUntilGone = true;
                    item.VelX = (outX / outLen) * _cfg.D("net.parry.launchSpeed");
                    item.VelY = (outY / outLen) * _cfg.D("net.parry.launchSpeed") * 0.35 + 1.2;
                    item.Pos.X += outX * 0.06;
                    item.Pos.Y += outY * 0.06;
                    _rimBouncedThisCatch = true;
                    _telemetry.Emit(now, "net.parry", new TelemetryPayload()
                        .Set("x", item.Pos.X).Set("y", item.Pos.Y)
                        .Set("speed", Fp.RoundNonNeg(netSpeed * 100) / 100));
                    return;
                }
                double b0 = _cfg.D("net.rim.bounceSpeed"), k0 = _cfg.D("net.rim.velTransfer");
                item.VelX = outX * b0 + VelX * k0;
                item.VelY = outY * b0 + VelY * k0;
                item.Pos.X += outX * 0.06;
                item.Pos.Y += outY * 0.06;
                _rimBouncedThisCatch = true;
                _telemetry.Emit(now, "net.rimhit", new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y)
                    .Set("heldCount", Held.Count).Set("netFull", IsFull));
                return;
            }
            double b = _cfg.D("net.rim.bounceSpeed"), k = _cfg.D("net.rim.velTransfer");
            item.VelX = outX * b + VelX * k;
            item.VelY = outY * b + VelY * k;
            item.Pos.X += outX * 0.06;
            item.Pos.Y += outY * 0.06;
            _rimBouncedThisCatch = true;
            if (_cfg.B("juggle.ladder.enabled"))
            {
                bool rungUp = TryArmRung(item, now);
                _telemetry.Emit(now, "net.rimhit", new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y)
                    .Set("heldCount", Held.Count).Set("netFull", IsFull)
                    .Set("rung", item.Rungs).Set("rungUp", rungUp));
            }
            else
            {
                _telemetry.Emit(now, "net.rimhit", new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y)
                    .Set("heldCount", Held.Count).Set("netFull", IsFull));
            }
        }

        public bool MouthPlaneCrossHit(FallingObject item, double prevItemX, double prevItemY, double dt,
            out CatchSweep.MouthCrossHit hit)
        {
            var pose = new CatchSweep.SweepPose
            {
                PrevItemX = prevItemX, PrevItemY = prevItemY,
                ItemX = item.Pos.X, ItemY = item.Pos.Y,
                PrevFacingX = PrevFacingX, PrevFacingY = PrevFacingY,
                FacingX = FacingX, FacingY = FacingY,
                PrevNetX = PrevX, PrevNetY = PrevY,
                NetX = Pos.X, NetY = Pos.Y,
            };
            return CatchSweep.FindMouthPlaneCross(_cfg, pose, item.Radius, dt, _cfg.D("world.fallGravity"), out hit);
        }

        bool CrossInStrikeZone(in CatchSweep.MouthCrossHit hit, double prevItemY, double itemY)
        {
            double maxY = _cfg.D("net.catch.maxY");
            if (hit.ItemY <= maxY) return true;
            return prevItemY > maxY && itemY <= maxY;
        }

        public bool IsGhostPass(FallingObject item, double prevItemX, double prevItemY)
        {
            if (item.State != FallState.Falling || item.Banked || IsFull) return false;
            if (_rimBouncedThisCatch) return false;
            if (!IsCatchCommitted()) return false;
            if (!MouthPlaneCrossHit(item, prevItemX, prevItemY, 1.0 / 60.0, out var hit)) return false;
            return CrossInStrikeZone(hit, prevItemY, item.Pos.Y);
        }

        public bool TryCatch(FallingObject item, double now, double prevItemX, double prevItemY, double dt)
        {
            _rimBouncedThisCatch = false;
            if (now < BrokenUntil) return false;
            if (item.State != FallState.Falling) return false;
            if (item.KoUntilGone) return false;
            bool fullRefusal = _cfg.B("net.heap.enabled")
                ? Held.Count >= Capacity + _cfg.I("net.heap.max")
                : IsFull;
            if (_cfg.B("net.heap.enabled") && now < item.ShedImmuneUntil) return false;
            if (fullRefusal && !_cfg.B("net.fullBounce.enabled")) return false;
            if (!IsCatchCommitted()) return false;

            if (!MouthPlaneCrossHit(item, prevItemX, prevItemY, dt, out var hit)
                || !CrossInStrikeZone(hit, prevItemY, item.Pos.Y))
            {
                if (_cfg.B("net.rim.enabled")) TryRimBounce(item, now);
                return false;
            }

            if (_cfg.B("items.charms.enabled") && item.Kind == ItemKind.Charm)
            {
                if (item.CharmContacts == 0)
                {
                    WebDeflect(item);
                    item.CharmContacts = 1;
                    _rimBouncedThisCatch = true;
                    TryArmRung(item, now);
                    _telemetry.Emit(now, "charm.charge", new TelemetryPayload()
                        .Set("x", item.Pos.X).Set("y", item.Pos.Y));
                    return false;
                }
                string effectName = "none";
                if (item.CharmEffect == CharmEffectKind.Life) effectName = "life";
                else if (item.CharmEffect == CharmEffectKind.Barrier) effectName = "barrier";
                else if (item.CharmEffect == CharmEffectKind.Slow) effectName = "slow";
                _telemetry.Emit(now, "charm.absorb", new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y).Set("effect", effectName));
                item.State = FallState.Landed;
                return false;
            }

            if (item.Kind == ItemKind.Knife && !item.KoUntilGone)
            {
                BreakNet(item, now);
                return false;
            }

            if (fullRefusal)
            {
                WebDeflect(item);
                _rimBouncedThisCatch = true;
                bool ladderOn = _cfg.B("juggle.ladder.enabled");
                bool rungUp = ladderOn && TryArmRung(item, now);
                var payload = new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y).Set("heldCount", Held.Count);
                if (ladderOn) { payload.Set("rung", item.Rungs).Set("rungUp", rungUp); }
                _telemetry.Emit(now, "net.fullbounce", payload);
                return false;
            }

            if (_cfg.B("items.taxonomy.enabled") && item.Kind == ItemKind.Bouncy && !item.WebBounced)
            {
                item.WebBounced = true;
                WebDeflect(item);
                _rimBouncedThisCatch = true;
                bool rungUp = TryArmRung(item, now);
                _telemetry.Emit(now, "net.webbounce", new TelemetryPayload()
                    .Set("x", item.Pos.X).Set("y", item.Pos.Y)
                    .Set("rung", item.Rungs).Set("rungUp", rungUp));
                return false;
            }

            item.State = FallState.Caught;
            item.Pos.X = hit.ItemX;
            item.Pos.Y = hit.ItemY;
            bool intoHeap = _cfg.B("net.heap.enabled") && Held.Count >= Capacity;
            if (intoHeap) item.Heaped = true;
            Held.Add(item);
            var catchPayload = new TelemetryPayload()
                .Set("heldCount", Held.Count).Set("sweepId", item.SweepId).Set("x", item.Pos.X);
            if (intoHeap) catchPayload.Set("heaped", true);
            if (_cfg.B("items.taxonomy.enabled") && item.Kind != ItemKind.Plain)
                catchPayload.Set("kind", item.Kind.ToString().ToLowerInvariant());
            _telemetry.Emit(now, "net.catch", catchPayload);
            return true;
        }

        public List<FallingObject> TryPour(double now, double returnStrokeStartedAt)
        {
            var empty = new List<FallingObject>();
            if (now < BrokenUntil) return empty;
            if (!_cfg.B("net.pour.enabled") || Held.Count == 0) return empty;
            bool nearBinX = Fp.Abs(Pos.X - _cfg.D("world.binX"))
                <= _cfg.D("world.binRadius") * _cfg.D("net.pour.xCapture");
            double hAbove = Pos.Y - _cfg.D("world.binY");
            bool heightOk = hAbove >= 0.12 && hAbove <= _cfg.D("net.pour.heightAbove");
            double fill = Vec3.Clamp01((double)Held.Count / Capacity);
            double weight = fill * Fp.Sqrt(fill);
            double downGate = Vec3.Lerpf(_cfg.D("net.pour.downSpeed"), _cfg.D("net.pour.downSpeedFull"), weight);
            bool strokingDown = VelY <= -downGate;
            double speed = Fp.Sqrt(VelX * VelX + VelY * VelY);
            double strokeTipY = speed > 0.5 ? VelY / speed : 1;
            bool tipped = FacingY <= _cfg.D("net.pour.faceDownY")
                || (strokingDown && strokeTipY <= _cfg.D("net.pour.strokeTipY"));
            if (!(nearBinX && heightOk && strokingDown && tipped))
            {
                bool pourAttempt = nearBinX && Held.Count > 0
                    && (strokingDown || hAbove <= _cfg.D("net.pour.heightAbove"));
                if (_cfg.B("diag.telemetry") && pourAttempt)
                {
                    string reason = !heightOk ? (hAbove < 0.12 ? "too_low" : "too_high")
                        : !strokingDown ? "not_stroking_down" : "mouth_not_tipped";
                    if (_lastPourReject != reason)
                    {
                        _lastPourReject = reason;
                        _telemetry.Emit(now, "net.pour_reject", new TelemetryPayload()
                            .Set("reason", reason).Set("heldCount", Held.Count));
                    }
                }
                return empty;
            }
            _lastPourReject = null;
            var outList = new List<FallingObject>(Held);
            Held.Clear();
            _telemetry.Emit(now, "net.dump", new TelemetryPayload()
                .Set("heldCount", outList.Count)
                .Set("returnStrokeMs", Fp.RoundToIntNonNeg((now - returnStrokeStartedAt) * 1000))
                .Set("method", "pour"));
            double toBinX = _cfg.D("world.binX") - Pos.X;
            double pourDown = Fp.Max(1.6, -VelY * 0.55);
            for (int i = 0; i < outList.Count; i++)
            {
                var item = outList[i];
                item.State = FallState.Falling;
                item.Banked = true;
                double spread = (i - (outList.Count - 1) / 2.0) * 0.12;
                item.Pos.Set(Pos.X + spread, Pos.Y - 0.15, 0);
                item.VelX = toBinX * 2.2 + spread * 0.4;
                item.VelY = -pourDown;
            }
            return outList;
        }

        public List<FallingObject> TryFlickDump(double now, double returnStrokeStartedAt)
        {
            var empty = new List<FallingObject>();
            if (now < BrokenUntil) return empty;
            if (!_cfg.B("net.flick.enabled") || Held.Count == 0) return empty;
            double speed = Fp.Sqrt(VelX * VelX + VelY * VelY);
            if (speed < _cfg.D("net.flick.minSpeed")) return empty;
            double bx = _cfg.D("world.binX") - Pos.X, by = _cfg.D("world.binY") - Pos.Y;
            double bLen = Fp.Sqrt(bx * bx + by * by);
            if (bLen < _cfg.D("world.binRadius")) return empty;
            double dot = (VelX * bx + VelY * by) / (speed * bLen);
            if (dot < _cfg.D("net.flick.cosHalfAngle")) return empty;
            var dumped = new List<FallingObject>(Held);
            Held.Clear();
            _telemetry.Emit(now, "net.dump", new TelemetryPayload()
                .Set("heldCount", dumped.Count)
                .Set("returnStrokeMs", Fp.RoundToIntNonNeg((now - returnStrokeStartedAt) * 1000))
                .Set("method", "flick"));
            return dumped;
        }

        public List<FallingObject> TryDump(double now, double returnStrokeStartedAt)
        {
            var empty = new List<FallingObject>();
            if (now < BrokenUntil) return empty;
            if (!_cfg.B("net.dump.dragEnabled") || Held.Count == 0) return empty;
            double dx = Pos.X - _cfg.D("world.binX");
            double dy = Pos.Y - _cfg.D("world.binY");
            if (Fp.Sqrt(dx * dx + dy * dy) > _cfg.D("world.binRadius")) return empty;
            var dumped = new List<FallingObject>(Held);
            Held.Clear();
            _telemetry.Emit(now, "net.dump", new TelemetryPayload()
                .Set("heldCount", dumped.Count)
                .Set("returnStrokeMs", Fp.RoundToIntNonNeg((now - returnStrokeStartedAt) * 1000))
                .Set("method", "drag"));
            return dumped;
        }
    }
}
