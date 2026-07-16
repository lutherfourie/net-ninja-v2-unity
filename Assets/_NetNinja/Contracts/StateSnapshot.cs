namespace NetNinja.Contracts
{
    /// <summary>Double-only read-model DTO emitted by Core each tick. View converts double→float.</summary>
    public sealed class StateSnapshot
    {
        public int Tick;
        public double Time;
        public string StateHash;
        public Vec3 NetPos;
        public Vec3 NetTarget;
        public double NetVelX;
        public double NetVelY;
        public double FacingX;
        public double FacingY;
        public int HeldCount;
        public int Capacity;
        public bool IsFull;
        public double Score;
        public double Combo;
        public int Lives;
        public int Misses;
        public int WaveCatches;
        public double Temper;
        public string Phase;
        public double CatX;
        public double CatIntensity;
        public bool GameOver;
    }
}
