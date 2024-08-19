// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public record AccessTokenResult(string AccessToken, AuthenticationResult AuthResult);

public class OktaClassicAccessTokenProvider(
    [FromKeyedServices(nameof(OktaHttpClientFactory))] HttpClient httpClient,
    OktaClassicAuthenticator authenticator)
{
    private const string OKTA_UI_CLIENT_ID = "okta.2b1959c8-bcc0-56eb-a589-cfcfb7422f26";

    /// <summary>
    ///     Get Okta API access token using Okta PKCE auth flow
    /// </summary>
    /// <param name="oktaDomain"></param>
    /// <param name="username"></param>
    /// <param name="password"></param>
    /// <param name="preferredMfa"></param>
    /// <returns></returns>
    public async Task<AccessTokenResult?> GetAccessTokenAsync(Uri oktaDomain, string username, string password, string? preferredMfa)
    {
        var (codeVerifier, codeChallenge) = CreatePkceCodes();

        // this call creates cookies needed by the token_redirect.
        await AuthorizeAsync(oktaDomain, codeChallenge);

        var authResult = await authenticator.AuthenticateAsync(oktaDomain, username, password, preferredMfa);

        await TokenRedirectAsync(oktaDomain, authResult.StateToken!);

        var authCode = await AuthorizeAsync(oktaDomain, codeChallenge);

        if (authCode is null)
            return null;

        var accessToken = await GetAccessToken(oktaDomain, authCode, codeVerifier);

        return new AccessTokenResult(accessToken, authResult);
    }

    private async Task<string?> AuthorizeAsync(Uri oktaDomain, string codeChallenge)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", OKTA_UI_CLIENT_ID },
            { "code_challenge", codeChallenge },
            { "code_challenge_method", "S256" },
            { "nonce", GetRandomString() },
            { "redirect_uri", new Uri(oktaDomain, "/enduser/callback").ToString() },
            { "response_type", "code" },
            { "state", GetRandomString() },
            { "scope", "openid profile email okta.users.read.self okta.enduser.dashboard.read" }
        };

        var url = new Uri(oktaDomain, "/oauth2/v1/authorize?" + GetQueryParams(parameters));
        using var httpResponse = await httpClient.GetAsync(url);

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
        using var httpResponse = await httpClient.GetAsync(url);

        if (httpResponse is { StatusCode: HttpStatusCode.Redirect or HttpStatusCode.TemporaryRedirect })
            return;

        httpResponse.EnsureSuccessStatusCode();
    }

    private async Task<string> GetAccessToken(Uri oktaDomain, string authCode, string codeVerifier)
    {
        var parameters = new Dictionary<string, string>
        {
            { "client_id", OKTA_UI_CLIENT_ID },
            { "redirect_uri", new Uri(oktaDomain, "/enduser/callback").ToString() },
            { "grant_type", "authorization_code" },
            { "code_verifier", codeVerifier },
            { "code", authCode }
        };

        using var httpResponse = await httpClient.PostAsync(new Uri(oktaDomain, "/oauth2/v1/token"), new FormUrlEncodedContent(parameters));
        var tokenResponse = await httpResponse.Content.ReadFromJsonAsync(OktaSourceGenerationContext.Default.TokenResponse);

        return tokenResponse?.AccessToken ?? throw new InvalidOperationException($"Invalid OAuth token response. Status Code: {httpResponse.StatusCode}");
    }

    private static string GetQueryParams(Dictionary<string, string> parameters) => string.Join("&", parameters.Select(kv => $"{kv.Key}={kv.Value}"));

    private static (string codeVerifier, string codeChallenge) CreatePkceCodes()
    {
        var codeVerifier = GetRandomString() + GetRandomString();

        var hashedCodeChallenge = SHA256.HashData(Encoding.UTF8.GetBytes(codeVerifier).ToArray());

        var codeChallenge = Convert.ToBase64String(hashedCodeChallenge)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');

        return (codeVerifier, codeChallenge);
    }

    private static string GetRandomString() => Guid.NewGuid().ToString("N");
}
