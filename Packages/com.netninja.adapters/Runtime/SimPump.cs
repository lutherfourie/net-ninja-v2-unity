using UnityEngine;
using VContainer.Unity;
using NetNinja.Core;

namespace NetNinja.Adapters
{
    /// <summary>IFixedTickable owner of DT=1/60 sim step.</summary>
    public sealed class SimPump : IFixedTickable
    {
        readonly Sim _sim;
        public SimPump(Sim sim) { _sim = sim; }
        public void FixedTick() { /* device input applied by DeviceInputSource; then _sim.Step() */ }
    }
}
