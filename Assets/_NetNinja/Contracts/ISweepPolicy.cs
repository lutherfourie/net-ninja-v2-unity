namespace NetNinja.Contracts
{
    public struct SweepDecision
    {
        public int Count;
        public double[] Xs;
        public int Direction; // 1 | -1
        public double StaggerSeconds;
        public bool IsSweep;
    }

    public struct SweepContext
    {
        public int Seed;
        public int CeremonyIndex;
        public double Derangement;
        public int FreeSlots;
        public int TotalCapacity;
        public double SimTime;
        public double LastSweepAt;
        public double NextSpawnX;
    }

    /// <summary>Strategy seam: Fair / Triage / Off. Implementations live in Core.</summary>
    public interface ISweepPolicy
    {
        string Name { get; }
        SweepDecision Decide(in SweepContext ctx, CoreConfigView cfg);
    }

    /// <summary>
    /// Read-only config surface for policy decisions without taking a Core dependency cycle.
    /// CoreConfig implements this; Contracts keeps the interface free of Unity.
    /// </summary>
    public interface CoreConfigView
    {
        double GetDouble(string key);
        bool GetBool(string key);
        string GetString(string key);
    }
}
