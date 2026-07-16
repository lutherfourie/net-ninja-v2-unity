using NetNinja.Contracts;
using UnityEngine;

namespace NetNinja.Adapters
{
    /// <summary>Input System → IInputReader (engine-coupled production binding).</summary>
    public sealed class DeviceInputSource : IInputReader
    {
        public InputFrame Read(int tick, double simTime)
        {
            // Skeleton: zero input; wire Input System actions in a follow-up.
            return new InputFrame(0, -2.5, pointerFollow: true, tick: tick);
        }
    }
}
