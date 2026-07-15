using NetNinja.Contracts;

namespace NetNinja.Adapters
{
    public sealed class StateProjector
    {
        public StateSnapshot Last { get; private set; }
        public void Project(StateSnapshot snap) => Last = snap;
    }
}
