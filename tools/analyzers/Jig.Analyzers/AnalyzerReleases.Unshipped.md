; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
DR0001 | Architecture | Error | Layer dependency violation, see LayerDependencyAnalyzer
DR0002 | Architecture | Error | Architecture ruleset is empty, see LayerDependencyAnalyzer
DR0003 | Architecture | Error | Architecture ruleset line is malformed, see LayerDependencyAnalyzer
DR0004 | Architecture | Error | Cross-module reference bypasses Contracts, see LayerDependencyAnalyzer
