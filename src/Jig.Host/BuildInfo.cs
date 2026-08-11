using System.Reflection;

namespace Jig.Host;

// What binary is actually running. The commit SHA is baked into the assembly's informational
// version at build time (SourceRevisionId, fed from the GIT_SHA build arg), so this reports the
// process itself rather than what a deploy label or image tag claims is running. That difference
// is the whole point on the day a rollback half-took or a stale replica is still serving.
public static class BuildInfo
{
    public static VersionInfo Current { get; } = Read();

    private static VersionInfo Read()
    {
        var informational = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion ?? "unknown";

        // MSBuild appends "+<SourceRevisionId>" to the informational version when the revision is
        // set, so a stamped build reads "1.0.0+abc1234" and splits into version and sha. A plain
        // local build has no "+", so the sha is honestly reported as unknown instead of faked.
        var plus = informational.IndexOf('+');
        return plus < 0
            ? new VersionInfo(informational, "unknown")
            : new VersionInfo(informational[..plus], informational[(plus + 1)..]);
    }
}

public record VersionInfo(string Version, string Sha);
