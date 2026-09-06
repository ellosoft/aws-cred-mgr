// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public static class OktaHttpClient
{
    private const string WINDOWS_USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";
    private const string MACOS_USER_AGENT = "Mozilla/5.0 (Macintosh; Intel Mac OS X 10.15; rv:128.0) Gecko/20100101 Firefox/128.0";
    private const string LINUX_USER_AGENT = "Mozilla/5.0 (X11; Linux x86_64; rv:128.0) Gecko/20100101 Firefox/128.0";

    public static void Configure(HttpClient httpClient)
    {
        httpClient.DefaultRequestHeaders.Add("User-Agent", WINDOWS_USER_AGENT);
    }

    /// <summary>
    ///     Desktop browser user agent matching the current platform. Okta Identity Engine uses the user agent platform
    ///     to decide how Okta Verify (FastPass) challenges are delivered (loopback ports, deep link scheme, download links)
    /// </summary>
    public static string GetPlatformUserAgent()
    {
        if (OperatingSystem.IsMacOS())
            return MACOS_USER_AGENT;

        if (OperatingSystem.IsWindows())
            return WINDOWS_USER_AGENT;

        return LINUX_USER_AGENT;
    }
}
