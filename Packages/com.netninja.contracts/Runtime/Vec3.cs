// Double-only portable vector. Mirrors net-lab packages/core/vec3.ts.
// Ops: + - * / and Math.Sqrt only via Fp helpers where needed.
namespace NetNinja.Contracts
{
    public struct Vec3
    {
        public double X;
        public double Y;
        public double Z;

        public Vec3(double x = 0, double y = 0, double z = 0)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vec3 Clone() => new Vec3(X, Y, Z);

        public void Set(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public void Copy(in Vec3 v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }

        public static Vec3 Add(in Vec3 a, in Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 Sub(in Vec3 a, in Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 Scale(in Vec3 a, double s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public static double Distance(in Vec3 a, in Vec3 b)
        {
            double dx = a.X - b.X, dy = a.Y - b.Y, dz = a.Z - b.Z;
            return System.Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        public static Vec3 Lerp(in Vec3 a, in Vec3 b, double t)
        {
            t = Clamp01(t);
            return new Vec3(a.X + (b.X - a.X) * t, a.Y + (b.Y - a.Y) * t, a.Z + (b.Z - a.Z) * t);
        }

        public static Vec3 MoveTowards(in Vec3 current, in Vec3 target, double maxDelta)
        {
            var d = Sub(target, current);
            double len = System.Math.Sqrt(d.X * d.X + d.Y * d.Y + d.Z * d.Z);
            if (len <= maxDelta || len == 0) return target.Clone();
            return Add(current, Scale(d, maxDelta / len));
        }

        public static double Clamp(double v, double min, double max)
            => v < min ? min : v > max ? max : v;

        public static double Clamp01(double v) => Clamp(v, 0, 1);

        public static double Lerpf(double a, double b, double t)
            => a + (b - a) * Clamp01(t);
    }
}
