// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public static class OktaHttpClientFactory
{
    /// <summary>
    ///     Create an HTTP client with cookie support to be used on Okta Classic authentication calls (PKCE auth flow)
    /// </summary>
    /// <returns></returns>
    public static HttpClient CreateHttpClient()
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

        return httpClient;
    }
}
