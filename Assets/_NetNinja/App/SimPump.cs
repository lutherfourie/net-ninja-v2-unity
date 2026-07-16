using NetNinja.Contracts;
using NetNinja.Core;

namespace NetNinja.Adapters
{
    /// <summary>
    /// Owner of the DT=1/60 sim step. Was an IFixedTickable under VContainer (ADR-0006); after the
    /// stack strip (ADR-0019) it is a plain class driven by NetNinja.App.Bootstrap.FixedUpdate.
    /// </summary>
    public sealed class SimPump
    {
        readonly Sim _sim;
        public SimPump(Sim sim) { _sim = sim; }

        /// <summary>Apply this tick's input, advance one fixed step, return the snapshot+hash.</summary>
        public StateSnapshot Step(InputFrame input) => _sim.Tick(input);
    }
}
