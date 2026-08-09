using System;
using System.Collections.Immutable;

namespace Jig.Analyzers;

/// <summary>
/// One line of the layer map: a namespace pattern that must not depend on another.
/// Patterns are wildcarded on the product prefix ("*.Api"), because layer names are
/// structural and the product name is not — the same map holds in every clone of the
/// template with no rename step.
/// </summary>
public readonly struct LayerRule
{
    private const string Wildcard = "*.";

    public string From { get; }
    public string To { get; }

    private LayerRule(string from, string to)
    {
        From = from;
        To = to;
    }

    /// <summary>Parse the layer map, discarding malformed-line detail. Prefer the two-argument overload
    /// wherever a malformed line must not vanish silently — see ADR 0009 / DR0003.</summary>
    public static ImmutableArray<LayerRule> Parse(string? text) => Parse(text, out _);

    /// <summary>
    /// Parse the layer map. Blank lines and "#" comments are ignored. Every other non-blank
    /// line must parse as "&lt;from&gt; -> &lt;to&gt;"; a line that does not is reported via
    /// <paramref name="malformedLines"/> instead of being dropped, so a typo cannot silently
    /// delete a rule (DR0003).
    /// </summary>
    public static ImmutableArray<LayerRule> Parse(string? text, out ImmutableArray<MalformedLine> malformedLines)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            malformedLines = ImmutableArray<MalformedLine>.Empty;
            return ImmutableArray<LayerRule>.Empty;
        }

        var rules = ImmutableArray.CreateBuilder<LayerRule>();
        var malformed = ImmutableArray.CreateBuilder<MalformedLine>();
        var lines = text!.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var rawLine = lines[i];
            var line = rawLine;
            var comment = line.IndexOf('#');
            if (comment >= 0) line = line.Substring(0, comment);

            if (line.Trim().Length == 0) continue; // blank line or comment-only line: not malformed

            var parts = line.Split(new[] { "->" }, StringSplitOptions.None);
            var from = parts.Length == 2 ? parts[0].Trim() : string.Empty;
            var to = parts.Length == 2 ? parts[1].Trim() : string.Empty;

            if (parts.Length != 2 || from.Length == 0 || to.Length == 0)
            {
                malformed.Add(new MalformedLine(i + 1, rawLine.Trim()));
                continue;
            }

            rules.Add(new LayerRule(from, to));
        }

        malformedLines = malformed.ToImmutable();
        return rules.ToImmutable();
    }

    /// <summary>True when a reference from <paramref name="fromNamespace"/> to <paramref name="toNamespace"/> breaks this rule.</summary>
    public bool Covers(string fromNamespace, string toNamespace) =>
        Matches(From, fromNamespace) && Matches(To, toNamespace);

    /// <summary>
    /// Match a namespace against a pattern. "*" stands for the prefix ahead of the layer
    /// segment(s) — one or more segments, never zero — so the same rule covers both a flat
    /// product namespace ("Jig.Infrastructure") and a modular one nested under a module
    /// segment ("Jig.Modules.Users.Infrastructure"). The layer segments may start at any
    /// index from 1 onward (never index 0, so the global namespace and a bare module root
    /// like "Jig.Modules.Users" are never themselves a layer), and segments after the layer
    /// run are allowed, so a sub-namespace nested under a layer
    /// ("Jig.Modules.Users.Infrastructure.Persistence.EfCore") still matches "*.Infrastructure"
    /// instead of silently escaping enforcement. This intentionally does not exclude
    /// same-shaped third-party namespaces like "Microsoft.EntityFrameworkCore.Infrastructure"
    /// by segment count or position; the caller guards against that by only evaluating symbols
    /// declared in the current compilation (see LayerDependencyAnalyzer.Inspect), which a
    /// metadata-only third-party type never is.
    /// A pattern without "*." is matched as a fixed, literal prefix instead (segment
    /// boundaries still apply, anchored at index 0), so "Jig.Domain" matches "Jig.Domain.Sub"
    /// but not "Acme.Domain".
    /// </summary>
    private static bool Matches(string pattern, string ns)
    {
        var segments = ns.Split('.');

        if (pattern.StartsWith(Wildcard, StringComparison.Ordinal))
        {
            // "*.Layer[.Rest]": the layer segments may appear starting at any index >= 1
            // (at least one prefix segment ahead of them, whatever its own depth turns out to
            // be), and segments after the layer run are allowed, so a sub-namespace nested
            // under the layer still matches.
            var layerSegments = pattern.Substring(Wildcard.Length).Split('.');

            for (var offset = 1; offset + layerSegments.Length <= segments.Length; offset++)
            {
                var isMatch = true;
                for (var i = 0; i < layerSegments.Length; i++)
                {
                    if (segments[offset + i] != layerSegments[i])
                    {
                        isMatch = false;
                        break;
                    }
                }

                if (isMatch) return true;
            }

            return false;
        }

        // No wildcard: the pattern itself must match a prefix of whole segments.
        var patternSegments = pattern.Split('.');
        if (segments.Length < patternSegments.Length) return false;

        for (var i = 0; i < patternSegments.Length; i++)
        {
            if (segments[i] != patternSegments[i]) return false;
        }

        return true;
    }
}

/// <summary>A line of ArchLayers.txt that is non-blank, non-comment, and did not parse as a rule.</summary>
public readonly struct MalformedLine
{
    public int LineNumber { get; }
    public string Text { get; }

    internal MalformedLine(int lineNumber, string text)
    {
        LineNumber = lineNumber;
        Text = text;
    }
}
