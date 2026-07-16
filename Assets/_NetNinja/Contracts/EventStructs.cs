// Typed event structs for the telemetry edge + View naming (ADR-0005).
// Core journal still uses name+payload; these are the adapter/view crossing types.
namespace NetNinja.Contracts
{
    public readonly struct SweepBeginEvent
    {
        public readonly int Count;
        public readonly double Span;
        public readonly int Direction;
        public readonly double Stagger;
        public readonly string Policy;
        public readonly int SweepId;
        public SweepBeginEvent(int count, double span, int direction, double stagger, string policy, int sweepId)
        {
            Count = count; Span = span; Direction = direction; Stagger = stagger; Policy = policy; SweepId = sweepId;
        }
    }

    public readonly struct WaveCatchEvent
    {
        public readonly int Count;
        public readonly int DurationMs;
        public WaveCatchEvent(int count, int durationMs) { Count = count; DurationMs = durationMs; }
    }

    public readonly struct DumpEvent
    {
        public readonly int HeldCount;
        public readonly int ReturnStrokeMs;
        public readonly string Method;
        public DumpEvent(int heldCount, int returnStrokeMs, string method)
        {
            HeldCount = heldCount; ReturnStrokeMs = returnStrokeMs; Method = method;
        }
    }

    public readonly struct MissDiagEvent
    {
        public readonly bool NetFull;
        public readonly int HeldCount;
        public readonly int FreeSlots;
        public readonly int Airborne;
        public readonly bool IsSweep;
        public MissDiagEvent(bool netFull, int heldCount, int freeSlots, int airborne, bool isSweep)
        {
            NetFull = netFull; HeldCount = heldCount; FreeSlots = freeSlots; Airborne = airborne; IsSweep = isSweep;
        }
    }

    public readonly struct FullEnterEvent
    {
        public readonly int HeldCount;
        public FullEnterEvent(int heldCount) { HeldCount = heldCount; }
    }

    public readonly struct FullExitEvent
    {
        public readonly int FullMs;
        public readonly int MissesDuring;
        public FullExitEvent(int fullMs, int missesDuring) { FullMs = fullMs; MissesDuring = missesDuring; }
    }

    public readonly struct RimHitEvent
    {
        public readonly double X;
        public readonly double Y;
        public readonly int HeldCount;
        public readonly bool NetFull;
        public RimHitEvent(double x, double y, int heldCount, bool netFull)
        {
            X = x; Y = y; HeldCount = heldCount; NetFull = netFull;
        }
    }

    public readonly struct PourRejectEvent
    {
        public readonly string Reason;
        public readonly int HeldCount;
        public PourRejectEvent(string reason, int heldCount) { Reason = reason; HeldCount = heldCount; }
    }
}
