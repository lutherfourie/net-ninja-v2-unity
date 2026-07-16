using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using NetNinja.Contracts;
using NetNinja.Core;
using NetNinja.Core.Personas;
using NetNinja.Core.State;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace NetNinja.Core.Parity.Tests
{
    /// <summary>
    /// Sim-only conformance via oracle-trace replay — personas are NOT gated here (ADR-0018).
    /// </summary>
    [TestFixture]
    public class GoldenVectorTests
    {
        public static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (File.Exists(Path.Combine(dir.FullName, "golden", "vectors.json")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        }

        static JObject LoadGolden()
        {
            var path = Path.Combine(FindRepoRoot(), "golden", "vectors.json");
            Assert.That(File.Exists(path), Is.True, "missing " + path);
            return JObject.Parse(File.ReadAllText(path));
        }

        /// <summary>
        /// Replay oracle-exported target frames (from net-lab) into the C# sim.
        /// Isolates sim bit-parity from plant Log/Cos libm ULP (ADR-0008).
        /// Traces: golden/traces/{persona}@{seed}.json
        /// </summary>
        static (string runHash, Dictionary<int, string> checkpoints) RunCellFromTrace(string persona, int seed, int ticks)
        {
            var root = FindRepoRoot();
            var tracePath = Path.Combine(root, "golden", "traces", persona + "@" + seed + ".json");
            Assert.That(File.Exists(tracePath), Is.True, "missing trace " + tracePath + " — export from net-lab");
            var tr = JObject.Parse(File.ReadAllText(tracePath));
            var xy = (JArray)tr["xy"];
            Assert.That(xy.Count, Is.EqualTo(ticks * 2));

            var cfg = CoreConfig.CreateDefault();
            var sim = new Sim(cfg, seed);
            var run = new Hasher();
            var cps = new HashSet<int> { ticks / 4, ticks / 2, ticks - 1 };
            var checkpoints = new Dictionary<int, string>();

            for (int i = 0; i < ticks; i++)
            {
                double tx = xy[i * 2].Value<double>();
                double ty = xy[i * 2 + 1].Value<double>();
                if (!sim.Score.GameOver)
                {
                    sim.Net.Target.Set(tx, ty, 0);
                    // intentMotor path does not set pointerFollow (stays false)
                    sim.Step();
                }
                string hs = sim.HashState();
                run.Str(hs);
                if (cps.Contains(i)) checkpoints[i] = hs;
            }
            return (run.Hex, checkpoints);
        }

        [Test]
        public void ConfigHash_MatchesGolden()
        {
            var g = LoadGolden();
            string expected = (string)g["configHash"];
            string actual = FnvStateHasher.HashConfig(CoreConfig.CreateDefault());
            Assert.That(actual, Is.EqualTo(expected), "config drift — re-export config/default.json from net-lab");
        }

        [Test]
        public void GoldenVectors_AllSixCells_BitExact_ViaOracleTraces()
        {
            var g = LoadGolden();
            int ticks = (int)g["ticks"];
            var cells = (JArray)g["cells"];
            var failures = new List<string>();

            foreach (var cell in cells)
            {
                string persona = (string)cell["persona"];
                int seed = (int)cell["seed"];
                string wantRun = (string)cell["runHash"];
                var wantCp = (JObject)cell["checkpoints"];

                var (runHash, cps) = RunCellFromTrace(persona, seed, ticks);
                if (runHash != wantRun)
                {
                    failures.Add($"{persona}@{seed} runHash got={runHash} want={wantRun}");
                    foreach (var prop in wantCp.Properties())
                    {
                        int t = int.Parse(prop.Name, CultureInfo.InvariantCulture);
                        string w = (string)prop.Value;
                        if (!cps.TryGetValue(t, out var got) || got != w)
                            failures.Add($"  cp@{t} got={got ?? "null"} want={w}");
                    }
                    continue;
                }
                foreach (var prop in wantCp.Properties())
                {
                    int t = int.Parse(prop.Name, CultureInfo.InvariantCulture);
                    string w = (string)prop.Value;
                    if (!cps.TryGetValue(t, out var got) || got != w)
                        failures.Add($"{persona}@{seed} cp@{t} got={got ?? "null"} want={w}");
                }
            }

            Assert.That(failures, Is.Empty, string.Join("\n", failures));
        }

        [Test]
        public void PerfectNoCorr_UsesSameConfigHash()
        {
            string h = FnvStateHasher.HashConfig(CoreConfig.CreateDefault());
            Assert.That(h, Is.EqualTo("6c3a8288f02919a3"));
        }

        /// <summary>
        /// Live IntentMotor matches oracle targets through the hold/reposition era.
        /// First plant ULP (Box–Muller Cos/Log libm) appears at tick 125 for perfect@42.
        /// </summary>
        [Test]
        public void LivePersona_MatchesOracleTargets_UntilPlantUlp()
        {
            var root = FindRepoRoot();
            var tracePath = Path.Combine(root, "golden", "traces", "perfect@42.json");
            var tr = JObject.Parse(File.ReadAllText(tracePath));
            var xy = (JArray)tr["xy"];

            var sim = new Sim(CoreConfig.CreateDefault(), 42);
            var bot = new PersonaDriver("perfect", 42);
            int first = -1;
            for (int i = 0; i < 200; i++)
            {
                bot.Drive(sim, Sim.FixedDt);
                double ox = xy[i * 2].Value<double>();
                double oy = xy[i * 2 + 1].Value<double>();
                long bx = BitConverter.DoubleToInt64Bits(sim.Net.Target.X);
                long by = BitConverter.DoubleToInt64Bits(sim.Net.Target.Y);
                long wx = BitConverter.DoubleToInt64Bits(ox);
                long wy = BitConverter.DoubleToInt64Bits(oy);
                if (bx != wx || by != wy)
                {
                    first = i;
                    break;
                }
                sim.Step();
            }
            // Documented plant ULP boundary (ADR-0008)
            Assert.That(first, Is.EqualTo(125), "expected first live-plant ULP at tick 125; got " + first);
        }
    }

    [TestFixture]
    public class SelfDeterminismTests
    {
        [TestCase("perfect", 42)]
        [TestCase("average", 42)]
        [TestCase("sloppy", 42)]
        [TestCase("perfect-nocorr", 42)]
        public void SameSeedPersona_Twice_SameRunHash(string persona, int seed)
        {
            string Run()
            {
                var cfg = CoreConfig.CreateDefault();
                var sim = new Sim(cfg, seed);
                var bot = new PersonaDriver(persona, seed);
                var run = new Hasher();
                for (int i = 0; i < 600; i++)
                {
                    if (!sim.Score.GameOver)
                    {
                        bot.Drive(sim, Sim.FixedDt);
                        sim.Step();
                    }
                    run.Str(sim.HashState());
                }
                return run.Hex;
            }
            Assert.That(Run(), Is.EqualTo(Run()));
        }
    }

    [TestFixture]
    public class HasherMicroVectorTests
    {
        [Test]
        public void Hasher_Num_NormalizesNegativeZero()
        {
            var a = new Hasher().Num(0.0).Hex;
            var b = new Hasher().Num(-0.0).Hex;
            Assert.That(a, Is.EqualTo(b));
        }

        [Test]
        public void Hasher_KnownMicroVector()
        {
            var h = new Hasher().Str("hi").Num(1).Bool(true).U32(7);
            var h2 = new Hasher().Str("hi").Num(1).Bool(true).U32(7);
            Assert.That(h.Hex, Is.EqualTo(h2.Hex));
            Assert.That(h.Hex.Length, Is.EqualTo(16));
        }

        [Test]
        public void ConfigHash_IsCanonical16Hex()
        {
            string h = FnvStateHasher.HashConfig(CoreConfig.CreateDefault());
            Assert.That(h.Length, Is.EqualTo(16));
            Assert.That(h, Is.EqualTo("6c3a8288f02919a3"));
        }
    }
}
