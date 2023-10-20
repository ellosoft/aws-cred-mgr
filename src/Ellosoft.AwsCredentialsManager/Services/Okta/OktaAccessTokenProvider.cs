// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public class OktaAccessTokenProvider
{
    private const string OKTA_UI_CLIENT_ID = "okta.2b1959c8-bcc0-56eb-a589-cfcfb7422f26";

    private readonly HttpClient _httpClient;
    private readonly OktaClassicAuthenticator _authenticator;

    public OktaAccessTokenProvider() : this(CreateHttpClient())
    {
    }

    public OktaAccessTokenProvider(HttpClient httpClient) : this(httpClient, new OktaClassicAuthenticator(httpClient))
    {
    }

    public OktaAccessTokenProvider(HttpClient httpClient, OktaClassicAuthenticator authenticator)
    {
        _httpClient = httpClient;
        _authenticator = authenticator;
    }

    public async Task<string?> GetAccessTokenAsync(Uri oktaDomain, string username, string password, string? preferredMfa)
    {
        var (codeVerifier, codeChallenge) = CreatePkceCodes();

        // this call creates cookies needed by the token_redirect.
        await AuthorizeAsync(oktaDomain, codeChallenge);

        var authResult = await _authenticator.AuthenticateAsync(oktaDomain, username, password, preferredMfa);

        await TokenRedirectAsync(oktaDomain, authResult.StateToken!);

        var authCode = await AuthorizeAsync(oktaDomain, codeChallenge);

        return await GetAccessToken(oktaDomain, authCode!, codeVerifier);
    }

    private async Task<string?> AuthorizeAsync(Uri oktaDomain, string codeChallenge)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id",  OKTA_UI_CLIENT_ID },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" },
            { "nonce", GetRandomString() },
            { "redirect_uri", new Uri(oktaDomain, "/enduser/callback").ToString() },
            { "response_type", "code" },
            { "state", GetRandomString() },
            { "scope", "openid profile email okta.users.read.self okta.enduser.dashboard.read" }
        };

        var url = new Uri(oktaDomain, "/oauth2/v1/authorize?" + GetQueryParams(parameters));
        var httpResponse = await _httpClient.GetAsync(url);

        if (httpResponse.IsSuccessStatusCode)
            return null;

        if (httpResponse is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.TemporaryRedirect })
        {
            var queryParameters = HttpUtility.ParseQueryString(httpResponse.Headers.Location?.Query ?? String.Empty);
            return queryParameters.Get("code");
        }

        throw new InvalidOperationException($"Invalid PKCE authorization response. Status Code: {httpResponse.StatusCode}");
    }

    private async Task TokenRedirectAsync(Uri oktaDomain, string stateToken)
    {
        var url = new Uri(oktaDomain, "/login/token/redirect?stateToken=" + stateToken);
        var httpResponse = await _httpClient.GetAsync(url);

        if (httpResponse is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.TemporaryRedirect })
            return;

        httpResponse.EnsureSuccessStatusCode();
    }

    private async Task<string?> GetAccessToken(Uri oktaDomain, string authCode, string codeVerifier)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", OKTA_UI_CLIENT_ID },
            { "redirect_uri", new Uri(oktaDomain, "/enduser/callback").ToString() },
            { "grant_type", "authorization_code" },
            { "code_verifier", codeVerifier },
            { "code", authCode }
        };

        var response = await _httpClient.PostAsync(new Uri(oktaDomain, "/oauth2/v1/token"), new FormUrlEncodedContent(parameters));

        var tokenData = await response.Content.ReadFromJsonAsync<Dictionary<string, object>>();
        return tokenData?["access_token"].ToString();
    }

    private static string GetQueryParams(Dictionary<string, string> parameters) => string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));

    private static (string codeVerifier, string codeChallenge) CreatePkceCodes()
    {
        var codeVerifier = "M25iVXpKU3puUjFaYWg3T1NDTDQtcW1ROUY5YXlwalNoc0hhakxifmZHag";

        var hashedCodeChallenge = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier).ToArray());

        var codeChallenge = Convert.ToBase64String(hashedCodeChallenge)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return (codeVerifier, codeChallenge);
    }

    private static HttpClient CreateHttpClient()
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };

        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);
        return httpClient;
    }

    private static string GetRandomString() => Guid.NewGuid().ToString("N");
}
