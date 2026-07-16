namespace NetNinja.Contracts
{
    /// <summary>Hand-rolled deterministic sink; Core writes, adapters drain.</summary>
    public interface ITelemetrySink
    {
        void Emit(double t, string name, TelemetryPayload payload);
    }

    public interface IEventBus
    {
        void Publish<T>(T evt) where T : struct;
    }

    /// <summary>Heterogeneous payload mirroring net-lab Record&lt;string, number|string|boolean&gt;.</summary>
    public sealed class TelemetryPayload
    {
        public readonly System.Collections.Generic.Dictionary<string, object> Data
            = new System.Collections.Generic.Dictionary<string, object>();

        public TelemetryPayload Set(string key, double v) { Data[key] = v; return this; }
        public TelemetryPayload Set(string key, int v) { Data[key] = (double)v; return this; }
        public TelemetryPayload Set(string key, bool v) { Data[key] = v; return this; }
        public TelemetryPayload Set(string key, string v) { Data[key] = v; return this; }

        public double GetDouble(string key, double d = 0)
            => Data.TryGetValue(key, out var o) && o is double x ? x : d;

        public string GetString(string key, string d = "")
            => Data.TryGetValue(key, out var o) && o is string s ? s : d;

        public bool GetBool(string key, bool d = false)
            => Data.TryGetValue(key, out var o) && o is bool b ? b : d;
    }

    public readonly struct TelemetryEvent
    {
        public readonly double T;
        public readonly string Name;
        public readonly TelemetryPayload Payload;

        public TelemetryEvent(double t, string name, TelemetryPayload payload)
        {
            T = t;
            Name = name;
            Payload = payload ?? new TelemetryPayload();
        }
    }
}
