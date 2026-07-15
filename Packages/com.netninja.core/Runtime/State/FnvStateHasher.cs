using System;
using System.Collections.Generic;
using System.Text;
using NetNinja.Contracts;

namespace NetNinja.Core.State
{
    /// <summary>
    /// Two-lane 32-bit FNV-1a → 16 hex chars. Port of net-lab packages/core/state.ts Hasher.
    /// See docs/hashing-spec.md.
    /// </summary>
    public sealed class Hasher
    {
        uint _a = 0x811c9dc5;
        uint _b = unchecked((uint)(0x811c9dc5 ^ 0x9e3779b9));
        readonly byte[] _dbl = new byte[8];

        void Byte(byte x)
        {
            unchecked
            {
                _a = (_a ^ x) * 0x01000193u;
                _b = (_b ^ x) * 0x01000197u;
            }
        }

        public Hasher U32(uint n)
        {
            Byte((byte)(n & 255));
            Byte((byte)((n >> 8) & 255));
            Byte((byte)((n >> 16) & 255));
            Byte((byte)((n >> 24) & 255));
            return this;
        }

        public Hasher U32(int n) => U32(unchecked((uint)n));

        /// <summary>IEEE-754 double LE; -0 normalized to +0 via (x + 0).</summary>
        public Hasher Num(double x)
        {
            double n = x + 0.0;
            // BitConverter is little-endian on target platforms for this project.
            var bits = BitConverter.GetBytes(n);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(bits);
            for (int i = 0; i < 8; i++) Byte(bits[i]);
            return this;
        }

        public Hasher Bool(bool v)
        {
            Byte(v ? (byte)1 : (byte)0);
            return this;
        }

        public Hasher Str(string s)
        {
            U32((uint)s.Length);
            for (int i = 0; i < s.Length; i++)
                U32((uint)s[i]);
            return this;
        }

        public string Hex
        {
            get
            {
                return _a.ToString("x8") + _b.ToString("x8");
            }
        }
    }

    public static class FnvStateHasher
    {
        static readonly string[] Phases = { "cooldown", "walk", "anticipate", "telegraph", "pushing" };
        static readonly string[] FallStates = { "falling", "caught", "landed" };

        public static string HashConfig(CoreConfig cfg)
        {
            var h = new Hasher();
            foreach (var k in cfg.SortedKeys())
            {
                h.Str(k);
                var v = cfg.Raw(k);
                if (v is double d) h.Num(d);
                else if (v is int i) h.Num(i);
                else if (v is long l) h.Num(l);
                else if (v is bool b) h.Bool(b);
                else h.Str(Convert.ToString(v));
            }
            return h.Hex;
        }

        static void HashTrackers(Hasher h, Dictionary<int, SweepTracker> m)
        {
            var keys = new List<int>(m.Keys);
            keys.Sort();
            h.U32((uint)keys.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                int k = keys[i];
                var t = m[k];
                h.Num(k).Num(t.Expected).Num(t.Caught).Num(t.StartedAt).Num(t.Deadline).Bool(t.Failed);
            }
        }

        public static string HashState(Sim sim)
        {
            var h = new Hasher();
            var net = sim.Net;
            var sc = sim.Score;
            var cat = sim.Cat;

            h.Num(sim.Time);

            h.Num(net.Pos.X).Num(net.Pos.Y);
            h.Num(net.Target.X).Num(net.Target.Y);
            h.Num(net.VelX).Num(net.VelY);
            h.Num(net.FacingX).Num(net.FacingY);
            h.Num(net.PrevX).Num(net.PrevY).Num(net.PrevFacingX).Num(net.PrevFacingY);
            h.Num(net.DynamicCapacity == null ? -1 : net.DynamicCapacity.Value);
            h.U32((uint)net.Held.Count);
            for (int i = 0; i < net.Held.Count; i++) h.Num(net.Held[i].Id);

            h.Num(sc.Score).Num(sc.Combo).Num(sc.Lives).Num(sc.Misses).Num(sc.WaveCatches).Num(sc.Temper);
            HashTrackers(h, sc.Sweeps);
            HashTrackers(h, sc.Trails);

            h.U32((uint)Array.IndexOf(Phases, WaveManager.PhaseName(cat.Phase)));
            h.Num(cat.CatX).Num(cat.PhaseEndsAt).Num(cat.NextSpawnAt);
            h.Num(cat.CeremonyIndex).Num(cat.LastSweepAt).Num(cat.PushedInCeremony).Num(cat.NextPushAt);
            h.Num(cat.CurrentSweepId).Num(cat.SweepSerial).Num(cat.CurrentTrailId).Num(cat.TrailSerial);
            h.Num(cat.ObjectSerial);
            if (cat.Pending.HasValue)
            {
                var p = cat.Pending.Value;
                h.Bool(true).Num(p.Count).Num(p.Direction).Num(p.StaggerSeconds).Bool(p.IsSweep);
                for (int i = 0; i < p.Xs.Length; i++) h.Num(p.Xs[i]);
            }
            else h.Bool(false);

            if (cat.Telegraph != null)
            {
                h.Bool(true).Num(cat.Telegraph.Direction).Num(cat.Telegraph.EndsAt);
                for (int i = 0; i < cat.Telegraph.Xs.Length; i++) h.Num(cat.Telegraph.Xs[i]);
            }
            else h.Bool(false);

            h.Num(sim.Tempo.CatIntensity);

            h.U32((uint)sim.Objects.Count);
            for (int i = 0; i < sim.Objects.Count; i++)
            {
                var o = sim.Objects[i];
                h.Num(o.Id)
                    .U32((uint)Array.IndexOf(FallStates, FallingObject.FallStateName(o.State)))
                    .Num(o.Pos.X).Num(o.Pos.Y).Num(o.VelY).Num(o.VelX)
                    .Num(o.SweepId).Num(o.TrailId).Bool(o.Banked).Num(o.Radius).Num(o.Value);
            }

            h.Num(sim.ReturnStrokeStartedAt).Bool(sim.WasFull);
            return h.Hex;
        }
    }
}
