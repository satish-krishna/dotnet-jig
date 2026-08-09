using System;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Jig.Analyzers;

/// <summary>
/// Enforces the layer map in ArchLayers.txt against the semantic model.
///
/// Layer dependency is a property of a graph, not of a file: .NET flows transitive
/// project references straight through, so a type can be in scope through an
/// intermediate project while every file a text search reads is innocent. Roslyn has
/// already resolved that graph — this reads it rather than rebuilding it.
///
/// All three diagnostics are NotConfigurable: the severity lives in compiled code, so
/// .editorconfig, NoWarn, and #pragma cannot switch them off. DR0001 reports a layer
/// violation; DR0002 and DR0003 guard the ruleset itself, which would otherwise be the
/// one thing a green build could not tell you about. See ADR 0009.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class LayerDependencyAnalyzer : DiagnosticAnalyzer
{
    public const string RulesetFileName = "ArchLayers.txt";

    internal static readonly DiagnosticDescriptor LayerViolation = new(
        id: "DR0001",
        title: "Layer dependency violation",
        messageFormat: "'{0}' must not depend on '{1}': the type '{2}' lives there",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "The layer map in ArchLayers.txt forbids this dependency. Fix the dependency, not the map.",
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    internal static readonly DiagnosticDescriptor EmptyRuleset = new(
        id: "DR0002",
        title: "Architecture ruleset is empty",
        messageFormat: "The architecture ruleset '{0}' is empty or missing; DR0001 enforced nothing",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A check that reports success because it found nothing to check is paperwork. Restore ArchLayers.txt.",
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

    internal static readonly DiagnosticDescriptor MalformedRule = new(
        id: "DR0003",
        title: "Architecture ruleset line is malformed",
        messageFormat: "ArchLayers.txt line {0} is not a rule: '{1}'. Expected '<from> -> <to>'.",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A line that fails to parse as a rule must fail the build, not vanish silently — a check that stays green because a typo quietly deleted a rule is paperwork. Fix the line in ArchLayers.txt.",
        customTags: new[] { WellKnownDiagnosticTags.NotConfigurable, WellKnownDiagnosticTags.CompilationEnd });

    // DR0002 and DR0003 are already taken (EmptyRuleset, MalformedRule above), so the
    // cross-module rule is DR0004, not DR0002 as its working name suggested.
    internal static readonly DiagnosticDescriptor CrossModuleViolation = new(
        id: "DR0004",
        title: "Cross-module reference bypasses Contracts",
        messageFormat: "'{0}' may reference module '{1}' only through '{1}.Contracts'; the type '{2}' is internal to that module",
        category: "Architecture",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true,
        description: "A module may depend on another module's Contracts, never its Domain/Application/Transport/Infrastructure internals. Route the reference through the referenced module's Contracts project instead.",
        customTags: WellKnownDiagnosticTags.NotConfigurable);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(LayerViolation, EmptyRuleset, MalformedRule, CrossModuleViolation);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    private static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        var rules = LayerRule.Parse(ReadRuleset(context.Options), out var malformedLines);
        if (rules.Length == 0)
        {
            // A check that goes green because its ruleset vanished is paperwork. Deletion is
            // neither Write nor Edit, so no PreToolUse hook can catch `rm ArchLayers.txt` —
            // this is the only guard that sees it. See ADR 0009.
            context.RegisterCompilationEndAction(end => end.ReportDiagnostic(
                Diagnostic.Create(EmptyRuleset, Location.None, RulesetFileName)));
            return;
        }

        if (malformedLines.Length > 0)
        {
            // A malformed line must fail the build on its own, even when the other lines
            // parse fine — otherwise rules.Length > 0 keeps DR0002 quiet and a typo silently
            // deletes exactly one rule. One diagnostic per malformed line, same as DR0001
            // reports once per violation.
            context.RegisterCompilationEndAction(end =>
            {
                foreach (var malformed in malformedLines)
                {
                    end.ReportDiagnostic(Diagnostic.Create(
                        MalformedRule, Location.None, malformed.LineNumber, malformed.Text));
                }
            });
        }

        // The product prefix is derived, not hardcoded, so the analyzer keeps working
        // unmodified if this template is ever cloned under a different product name. It is
        // the same string whichever project in the solution is compiling: AssemblyName is
        // "Jig.Modules.Users", "Jig.Host", "Jig.SharedKernel", etc., all sharing "Jig" as the
        // first dot-segment.
        var assemblyName = context.Compilation.AssemblyName ?? string.Empty;
        var productPrefix = assemblyName.Split('.')[0];

        context.RegisterSyntaxNodeAction(
            node => Inspect(node, rules, productPrefix),
            SyntaxKind.IdentifierName,
            SyntaxKind.GenericName,
            SyntaxKind.QualifiedName);
    }

    private static string? ReadRuleset(AnalyzerOptions options) =>
        options.AdditionalFiles
            .FirstOrDefault(file => Path.GetFileName(file.Path) == RulesetFileName)
            ?.GetText()?.ToString();

    private static void Inspect(SyntaxNodeAnalysisContext context, ImmutableArray<LayerRule> rules, string productPrefix)
    {
        // A QualifiedNameSyntax chain (type position, e.g. "Jig.Infrastructure.Outer.Inner")
        // produces one node per segment, but every segment resolves within the same
        // namespace — so skip inner segments and analyze only the outermost node, which
        // names the type actually referenced. That avoids reporting a nested type once per
        // segment instead of once.
        //
        // A MemberAccessExpressionSyntax chain (expression position, e.g.
        // "Jig.Infrastructure.JigDbContext.Label.ToString()") is different: each link
        // resolves to a DIFFERENT type in a DIFFERENT namespace. "ToString" resolves to
        // System.String; only "JigDbContext" resolves to the forbidden type. Skipping inner
        // links here would silently miss exactly the link that matters, so every link in a
        // member-access chain is inspected on its own — even though that means a single
        // violation can report more than once when several links in the same chain each
        // name a forbidden type. Noise beats silence.
        if (context.Node.Parent is QualifiedNameSyntax) return;

        var from = context.ContainingSymbol?.ContainingNamespace?.ToDisplayString();
        if (from is null) return;

        var symbol = context.SemanticModel.GetSymbolInfo(context.Node).Symbol;
        var type = symbol as ITypeSymbol ?? symbol?.ContainingType;

        if (type is null) return;

        // Only first-party (product) types can violate the layer map or the module boundary.
        // A metadata-only reference (any NuGet package, the BCL) belongs to some other
        // assembly and is excluded here, even if its namespace happens to end in a
        // layer-shaped segment (e.g. Microsoft.EntityFrameworkCore.Infrastructure). This used
        // to be gated on type.Locations.Any(IsInSource), which also (wrongly) excluded
        // cross-project first-party references: a type from another Jig.Modules.* assembly is
        // metadata to THIS compilation, so IsInSource was always false for it and cross-module
        // violations could never fire. Gating on assembly membership instead of source
        // location keeps third parties out while letting first-party cross-project references
        // through for inspection below.
        var referencedAssemblyName = type.ContainingAssembly?.Name;
        var isProductAssembly = referencedAssemblyName is not null &&
            (referencedAssemblyName == productPrefix ||
             referencedAssemblyName.StartsWith(productPrefix + ".", StringComparison.Ordinal));
        if (!isProductAssembly) return;

        var to = type.ContainingNamespace?.ToDisplayString();
        if (to is null) return;

        // A module may depend on another module only through that module's Contracts —
        // never its Domain/Application/Transport/Infrastructure internals. This is a separate
        // axis from the intra-module layer map below: a cross-module reference is governed by
        // DR0004 alone, not by DR0001, so same-module layer rules never run against it and a
        // legitimate Contracts reference never gets layer-checked against the wrong module's
        // layer map.
        var sourceModule = LayerRule.ModuleOf(from);
        var targetModule = LayerRule.ModuleOf(to);
        if (sourceModule is not null && targetModule is not null && sourceModule != targetModule)
        {
            if (!LayerRule.IsContractsOf(to, targetModule))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    CrossModuleViolation,
                    context.Node.GetLocation(),
                    $"{productPrefix}.Modules.{sourceModule}",
                    targetModule,
                    type.Name));
            }

            return;
        }

        foreach (var rule in rules)
        {
            if (!rule.Covers(from, to)) continue;

            context.ReportDiagnostic(Diagnostic.Create(
                LayerViolation, context.Node.GetLocation(), rule.From, rule.To, type!.Name));
            return;
        }
    }
}
