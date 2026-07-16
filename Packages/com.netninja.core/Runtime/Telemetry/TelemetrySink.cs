using System;
using System.Collections.Generic;
using NetNinja.Contracts;

namespace NetNinja.Core.Telemetry
{
    public sealed class TelemetrySink : ITelemetrySink
    {
        public readonly List<TelemetryEvent> Events = new List<TelemetryEvent>();
        readonly List<Action<TelemetryEvent>> _listeners = new List<Action<TelemetryEvent>>();

        public void Emit(double t, string name, TelemetryPayload payload = null)
        {
            var e = new TelemetryEvent(t, name, payload ?? new TelemetryPayload());
            Events.Add(e);
            for (int i = 0; i < _listeners.Count; i++)
                _listeners[i](e);
        }

        public void On(Action<TelemetryEvent> listener) => _listeners.Add(listener);
    }

    public sealed class EventJournal
    {
        public readonly List<TelemetryEvent> Entries = new List<TelemetryEvent>();

        public void Append(TelemetryEvent e) => Entries.Add(e);

        public void Clear() => Entries.Clear();
    }
}
