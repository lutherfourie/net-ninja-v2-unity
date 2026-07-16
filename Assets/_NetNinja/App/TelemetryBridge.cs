using NetNinja.Core.Telemetry;

namespace NetNinja.Adapters
{
    /// <summary>Drains the Core journal to the typed telemetry sink with run-context stamping (edge only).</summary>
    public sealed class TelemetryBridge
    {
        public void Drain(TelemetrySink sink)
        {
            // Skeleton: fan-out is a plain List&lt;Action&lt;T&gt;&gt; on the sink (the DI/Rx/bus triad was
            // dropped in ADR-0019); a message bus is re-addable only if many live subscribers appear.
            _ = sink;
        }
    }
}
