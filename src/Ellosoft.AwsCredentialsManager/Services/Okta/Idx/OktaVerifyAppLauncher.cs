// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

public interface IOktaVerifyAppLauncher
{
    /// <summary>
    ///     Opens an Okta Verify deep link (e.g. com-okta-authenticator:/deviceChallenge?challengeRequest=...) with the OS,
    ///     launching the Okta Verify desktop app
    /// </summary>
    /// <returns>true if the OS accepted the request</returns>
    bool TryLaunch(string uri);
}

public class OktaVerifyAppLauncher(ILogger<OktaVerifyAppLauncher> logger) : IOktaVerifyAppLauncher
{
    public bool TryLaunch(string uri)
    {
        try
        {
            var startInfo = CreateStartInfo(uri);

            using var process = Process.Start(startInfo);

            if (process is null)
                return false;

            if (OperatingSystem.IsWindows())
                return true;

            // 'open' / 'xdg-open' exit with a non-zero code when no application handles the URI scheme
            return process.WaitForExit(TimeSpan.FromSeconds(10)) && process.ExitCode == 0;
        }
        catch (Exception e) when (e is Win32Exception or InvalidOperationException or IOException or PlatformNotSupportedException)
        {
            logger.LogWarning(e, "Unable to launch Okta Verify using URI scheme {Scheme}", uri.Split(':')[0]);

            return false;
        }
    }

    private static ProcessStartInfo CreateStartInfo(string uri)
    {
        if (OperatingSystem.IsWindows())
            return new ProcessStartInfo(uri) { UseShellExecute = true };

        var launcher = OperatingSystem.IsMacOS() ? "open" : "xdg-open";

        return new ProcessStartInfo(launcher, [uri])
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };
    }
}
