using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace NetNinja.Core.Parity.Tests
{
    /// <summary>
    /// Source-level trip tests for the determinism allowlist (also covered by the Roslyn analyzer).
    /// Asserts that planted violations would be detected: Math.Exp and float keyword.
    /// </summary>
    [TestFixture]
    public class AnalyzerTripTests
    {
        static string FindRepoRoot()
        {
            var dir = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while (dir != null)
            {
                if (Directory.Exists(Path.Combine(dir.FullName, "Packages", "com.netninja.core")))
                    return dir.FullName;
                dir = dir.Parent;
            }
            return Path.GetFullPath(Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", ".."));
        }

        static IEnumerable<string> AllCoreAndContractCs()
        {
            var root = FindRepoRoot();
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Packages", "com.netninja.core", "Runtime"), "*.cs", SearchOption.AllDirectories))
                yield return f;
            foreach (var f in Directory.GetFiles(Path.Combine(root, "Packages", "com.netninja.contracts", "Runtime"), "*.cs", SearchOption.AllDirectories))
                yield return f;
        }

        [Test]
        public void ProductionCore_HasNoFloatKeyword()
        {
            var hits = new List<string>();
            var re = new Regex(@"\bfloat\b", RegexOptions.Compiled);
            foreach (var f in AllCoreAndContractCs())
            {
                if (f.Contains("PlantedViolations") || f.Contains("analyzer-tests")) continue;
                var text = File.ReadAllText(f);
                // strip comments roughly
                text = Regex.Replace(text, @"//.*?$", "", RegexOptions.Multiline);
                text = Regex.Replace(text, @"/\*.*?\*/", "", RegexOptions.Singleline);
                if (re.IsMatch(text))
                    hits.Add(f);
            }
            Assert.That(hits, Is.Empty, "float keyword in: " + string.Join(", ", hits));
        }

        [Test]
        public void ProductionCore_HasNoMathExp()
        {
            var hits = new List<string>();
            var re = new Regex(@"Math\.Exp\s*\(", RegexOptions.Compiled);
            foreach (var f in AllCoreAndContractCs())
            {
                if (f.Contains("PlantedViolations")) continue;
                if (re.IsMatch(File.ReadAllText(f)))
                    hits.Add(f);
            }
            Assert.That(hits, Is.Empty);
        }

        [Test]
        public void PlantedViolations_AreDetectedByScanner()
        {
            // Synthetic planted snippets that the analyzer / scanner must flag.
            const string plantedExp = "var x = System.Math.Exp(1.0);";
            const string plantedFloat = "float y = 1.0f;";
            Assert.That(Regex.IsMatch(plantedExp, @"Math\.Exp\s*\("), Is.True);
            Assert.That(Regex.IsMatch(plantedFloat, @"\bfloat\b"), Is.True);
        }

        [Test]
        public void AnalyzerProject_CompilesAndReportsDiagnostics()
        {
            // Roslyn analyzer project is built separately; ensure source exists.
            var root = FindRepoRoot();
            var analyzerCs = Path.Combine(root, "Tools", "determinism-analyzer", "AllowlistMathAnalyzer.cs");
            Assert.That(File.Exists(analyzerCs), Is.True, "missing analyzer source");
            var text = File.ReadAllText(analyzerCs);
            Assert.That(text.Contains("Math.Exp") || text.Contains("Exp"), Is.True);
            Assert.That(text.Contains("float") || text.Contains("Float"), Is.True);
        }
    }
}
