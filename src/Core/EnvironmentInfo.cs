using System.Runtime.InteropServices;

namespace Core;

public sealed record EnvironmentReport(
    string OsDescription,
    string OsVersion,
    string ProcessArchitecture,
    string DotNetVersion,
    string FrameworkDescription,
    string BaseDirectory,
    string CurrentDirectory,
    string SubjectArea,
    string DetectedRid,
    string ReportedRid,
    string BuildNote
);

public static class EnvironmentInfo
{
#if NET10_0_OR_GREATER
    private const string BuildNote = "збірка під net10.0";
#else
    private const string BuildNote = "збірка під net8.0";
#endif
    public static EnvironmentReport Collect()
    {
        return new EnvironmentReport(
            RuntimeInformation.OSDescription,
            Environment.OSVersion.ToString(),
            RuntimeInformation.ProcessArchitecture.ToString(),
            Environment.Version.ToString(),
            RuntimeInformation.FrameworkDescription,
            AppContext.BaseDirectory,
            Environment.CurrentDirectory,
            "Бібліотека — облік видач примірників книг читачам.",
            DetectRid(),
            RuntimeInformation.RuntimeIdentifier,
            BuildNote
        );
    }

    private static string DetectRid()
    {
        string os = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "win" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "linux" :
                   RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "osx" : "unknown";

        string arch = RuntimeInformation.ProcessArchitecture switch
        {
            Architecture.X64 => "x64",
            Architecture.X86 => "x86",
            Architecture.Arm64 => "arm64",
            Architecture.Arm => "arm",
            _ => "unknown"
        };

        return $"{os}-{arch}";
    }
}