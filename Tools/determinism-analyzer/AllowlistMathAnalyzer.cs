using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace NetNinja.Determinism.Analyzer
{
    /// <summary>
    /// Allowlist analyzer for NetNinja.Contracts + NetNinja.Core (+ personas).
    /// Permits System.Math.{Sqrt,Abs,Min,Max,Floor,Ceiling,Truncate} and provisional
    /// plant set {Log,Log2,Cos} (ADR-0008). Errors on float, Mathf, UnityEngine.*,
    /// Burst, Unity.Mathematics, and every other System.Math member (incl. Exp).
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class AllowlistMathAnalyzer : DiagnosticAnalyzer
    {
        public const string FloatId = "NNDET001";
        public const string MathId = "NNDET002";
        public const string UnityId = "NNDET003";

        static readonly DiagnosticDescriptor FloatRule = new DiagnosticDescriptor(
            FloatId, "float forbidden in deterministic core",
            "Type 'float' is forbidden in Contracts/Core (use double)",
            "Determinism", DiagnosticSeverity.Error, isEnabledByDefault: true);

        static readonly DiagnosticDescriptor MathRule = new DiagnosticDescriptor(
            MathId, "System.Math member not on allowlist",
            "System.Math.{0} is not allowlisted (permit Sqrt/Abs/Min/Max/Floor/Ceiling/Truncate; plant: Log/Log2/Cos)",
            "Determinism", DiagnosticSeverity.Error, isEnabledByDefault: true);

        static readonly DiagnosticDescriptor UnityRule = new DiagnosticDescriptor(
            UnityId, "Engine type forbidden in deterministic core",
            "'{0}' is forbidden in Contracts/Core",
            "Determinism", DiagnosticSeverity.Error, isEnabledByDefault: true);

        static readonly ImmutableHashSet<string> AllowedMath = ImmutableHashSet.Create(
            "Sqrt", "Abs", "Min", "Max", "Floor", "Ceiling", "Truncate",
            // provisional plant (ADR-0008)
            "Log", "Log2", "Cos");

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
            => ImmutableArray.Create(FloatRule, MathRule, UnityRule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            // Scope to engine-free compilations only (Contracts/Core + pure-.NET parity harness).
            // View/App/Editor intentionally use float/UnityEngine and must not be gated.
            context.RegisterCompilationStartAction(start =>
            {
                if (start.Compilation.GetTypeByMetadataName("UnityEngine.Object") != null)
                    return;
                start.RegisterSyntaxNodeAction(AnalyzePredefinedType, SyntaxKind.PredefinedType);
                start.RegisterSyntaxNodeAction(AnalyzeMemberAccess, SyntaxKind.SimpleMemberAccessExpression);
                start.RegisterSyntaxNodeAction(AnalyzeIdentifier, SyntaxKind.IdentifierName);
            });
        }

        static void AnalyzePredefinedType(SyntaxNodeAnalysisContext ctx)
        {
            var node = (PredefinedTypeSyntax)ctx.Node;
            if (node.Keyword.IsKind(SyntaxKind.FloatKeyword))
                ctx.ReportDiagnostic(Diagnostic.Create(FloatRule, node.GetLocation()));
        }

        static void AnalyzeMemberAccess(SyntaxNodeAnalysisContext ctx)
        {
            var ma = (MemberAccessExpressionSyntax)ctx.Node;
            var sym = ctx.SemanticModel.GetSymbolInfo(ma).Symbol;
            if (sym == null) return;

            // System.Math.X
            if (sym.ContainingType != null
                && sym.ContainingType.ToDisplayString() == "System.Math"
                && sym.Kind == SymbolKind.Method)
            {
                string name = sym.Name;
                if (!AllowedMath.Contains(name))
                    ctx.ReportDiagnostic(Diagnostic.Create(MathRule, ma.Name.GetLocation(), name));
            }

            // Mathf / UnityEngine / Burst / Unity.Mathematics
            string typeName = sym.ContainingType?.ToDisplayString() ?? "";
            if (typeName.StartsWith("UnityEngine")
                || typeName.StartsWith("Unity.Mathematics")
                || typeName.StartsWith("Unity.Burst")
                || typeName == "UnityEngine.Mathf"
                || typeName.EndsWith("Mathf"))
            {
                ctx.ReportDiagnostic(Diagnostic.Create(UnityRule, ma.GetLocation(), typeName));
            }
        }

        static void AnalyzeIdentifier(SyntaxNodeAnalysisContext ctx)
        {
            var id = (IdentifierNameSyntax)ctx.Node;
            if (id.Identifier.Text == "Mathf")
                ctx.ReportDiagnostic(Diagnostic.Create(UnityRule, id.GetLocation(), "Mathf"));
        }
    }
}
