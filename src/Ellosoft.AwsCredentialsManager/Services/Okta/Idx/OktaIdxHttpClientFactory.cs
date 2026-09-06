// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

public interface IOktaIdxHttpClientFactory
{
    /// <summary>
    ///     Creates an HTTP client for a single Okta Identity Engine sign-in transaction: uses the given cookie jar
    ///     (Okta device/session cookies), follows redirects and identifies itself as a desktop browser
    ///     (Okta selects the FastPass challenge method based on the user agent platform)
    /// </summary>
    HttpClient CreateSessionClient(CookieContainer cookieContainer);

    /// <summary>
    ///     Creates an HTTP client to talk to the Okta Verify loopback server on localhost
    /// </summary>
    HttpClient CreateLoopbackClient();
}

public class OktaIdxHttpClientFactory : IOktaIdxHttpClientFactory
{
    public HttpClient CreateSessionClient(CookieContainer cookieContainer)
    {
        var handler = new HttpClientHandler
        {
            UseCookies = true,
            CookieContainer = cookieContainer,
            AllowAutoRedirect = true
        };

        var httpClient = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(60) };
        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(OktaHttpClient.GetPlatformUserAgent());

        return httpClient;
    }

    public HttpClient CreateLoopbackClient()
    {
        // never send loopback traffic through a proxy and never carry cookies to Okta Verify
        var handler = new HttpClientHandler
        {
            UseCookies = false,
            UseProxy = false,
            AllowAutoRedirect = false
        };

        return new HttpClient(handler) { Timeout = TimeSpan.FromMinutes(5) };
    }
}
