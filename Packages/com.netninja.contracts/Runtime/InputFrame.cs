namespace NetNinja.Contracts
{
    /// <summary>Per-tick input at DT=1/60. Production: device; parity: persona/replay.</summary>
    public struct InputFrame
    {
        public double TargetX;
        public double TargetY;
        public bool PointerFollow;
        public int Tick;

        public InputFrame(double targetX, double targetY, bool pointerFollow = false, int tick = 0)
        {
            TargetX = targetX;
            TargetY = targetY;
            PointerFollow = pointerFollow;
            Tick = tick;
        }
    }

    public interface IInputReader
    {
        InputFrame Read(int tick, double simTime);
    }
}
