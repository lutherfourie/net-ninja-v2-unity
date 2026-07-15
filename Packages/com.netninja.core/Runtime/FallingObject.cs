using NetNinja.Contracts;

namespace NetNinja.Core
{
    public enum FallState
    {
        Falling = 0,
        Caught = 1,
        Landed = 2,
    }

    public enum ItemKind
    {
        Plain,
        Bouncy,
        Golden,
        Knife,
        Charm,
    }

    public enum CharmEffectKind
    {
        None,
        Barrier,
        Slow,
        Life,
    }

    public sealed class FallingObject
    {
        public readonly int Id;
        public FallState State = FallState.Falling;
        public Vec3 Pos;
        public double VelY;
        public double VelX;
        public int SweepId;
        public int TrailId;
        public bool Banked;
        public int Rungs;
        public double LastRungAt = -1;
        public ItemKind Kind = ItemKind.Plain;
        public bool WebBounced;
        public bool Heaped;
        public double ShedImmuneUntil = -1;
        public bool KoUntilGone;
        public int CharmContacts;
        public CharmEffectKind CharmEffect = CharmEffectKind.None;
        public readonly double Radius;
        public readonly double Value;

        public FallingObject(double x, double y, double radius = 0.28, double value = 10, int id = 0)
        {
            Pos = new Vec3(x, y, 0);
            Radius = radius;
            Value = value;
            Id = id;
        }

        public FallState Step(double dt, double gravity, double floorY)
        {
            if (State != FallState.Falling) return State;
            VelY += gravity * dt;
            Pos.Y += VelY * dt;
            Pos.X += VelX * dt;
            VelX *= 1 - 0.5 * dt;
            if (Pos.Y <= floorY)
            {
                Pos.Y = floorY;
                State = FallState.Landed;
            }
            return State;
        }

        public static string FallStateName(FallState s)
        {
            switch (s)
            {
                case FallState.Falling: return "falling";
                case FallState.Caught: return "caught";
                case FallState.Landed: return "landed";
                default: return "falling";
            }
        }
    }
}
