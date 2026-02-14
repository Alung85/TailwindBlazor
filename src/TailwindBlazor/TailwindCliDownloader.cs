using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;

namespace TailwindBlazor;

public static class TailwindCliDownloader
{
    private static readonly HttpClient HttpClient = new();

    public static string GetCacheDirectory(string version)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".tailwindblazor", "cli", version);
    }

    public static string GetPlatformIdentifier()
    {
        var arch = RuntimeInformation.OSArchitecture;

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return arch == Architecture.X64 ? "windows-x64" : throw new PlatformNotSupportedException($"Windows {arch} is not supported.");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            return arch switch
            {
                Architecture.Arm64 => "macos-arm64",
                Architecture.X64 => "macos-x64",
                _ => throw new PlatformNotSupportedException($"macOS {arch} is not supported.")
            };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return arch switch
            {
                Architecture.X64 => "linux-x64",
                Architecture.Arm64 => "linux-arm64",
                _ => throw new PlatformNotSupportedException($"Linux {arch} is not supported.")
            };

        throw new PlatformNotSupportedException("Unsupported operating system.");
    }

    public static string GetBinaryName(string platform)
    {
        var extension = platform.StartsWith("windows", StringComparison.Ordinal) ? ".exe" : "";
        return $"tailwindcss-{platform}{extension}";
    }

    public static string GetDownloadUrl(string version, string binaryName)
    {
        return $"https://github.com/tailwindlabs/tailwindcss/releases/download/v{version}/{binaryName}";
    }

    public static string ResolveCliPath(TailwindOptions options)
    {
        if (!string.IsNullOrEmpty(options.CliPath))
            return options.CliPath;

        var platform = GetPlatformIdentifier();
        var binaryName = GetBinaryName(platform);
        var cacheDir = GetCacheDirectory(options.TailwindVersion);
        return Path.Combine(cacheDir, binaryName);
    }

    public static async Task EnsureCliAsync(TailwindOptions options, ILogger? logger = null, CancellationToken cancellationToken = default)
    {
        var cliPath = ResolveCliPath(options);

        if (File.Exists(cliPath))
        {
            logger?.LogDebug("Tailwind CLI already cached at {Path}", cliPath);
            return;
        }

        var platform = GetPlatformIdentifier();
        var binaryName = GetBinaryName(platform);
        var downloadUrl = GetDownloadUrl(options.TailwindVersion, binaryName);
        var cacheDir = Path.GetDirectoryName(cliPath)!;

        Directory.CreateDirectory(cacheDir);

        logger?.LogInformation("TailwindBlazor: Downloading Tailwind CLI v{Version} for {Platform}...", options.TailwindVersion, platform);

        using var response = await HttpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();

        await using var fileStream = new FileStream(cliPath, FileMode.Create, FileAccess.Write, FileShare.None);
        await response.Content.CopyToAsync(fileStream, cancellationToken);

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            File.SetUnixFileMode(cliPath,
                UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute |
                UnixFileMode.GroupRead | UnixFileMode.GroupExecute |
                UnixFileMode.OtherRead | UnixFileMode.OtherExecute);
        }

        logger?.LogInformation("TailwindBlazor: CLI downloaded to {Path}", cliPath);
    }
}
