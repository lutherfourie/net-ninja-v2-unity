using NetNinja.Core.Telemetry;

namespace NetNinja.Adapters
{
    /// <summary>Drains Core journal → MessagePipe with run-context stamping (edge only).</summary>
    public sealed class TelemetryBridge
    {
        public void Drain(TelemetrySink sink)
        {
            // Skeleton: MessagePipe IPublisher registration generated later.
            _ = sink;
        }
    }
}
