using System.Collections.Generic;
using NetNinja.Contracts;

namespace NetNinja.Core.Personas
{
    /// <summary>Engine-free replay of per-tick InputFrames (human capture resampled offline).</summary>
    public sealed class ReplayDriver : IInputReader
    {
        readonly IReadOnlyList<InputFrame> _frames;

        public ReplayDriver(IReadOnlyList<InputFrame> frames)
        {
            _frames = frames ?? new InputFrame[0];
        }

        public InputFrame Read(int tick, double simTime)
        {
            if (_frames.Count == 0)
                return new InputFrame(0, -2.5, false, tick);
            int idx = tick < _frames.Count ? tick : _frames.Count - 1;
            var f = _frames[idx];
            return new InputFrame(f.TargetX, f.TargetY, f.PointerFollow, tick);
        }
    }
}
