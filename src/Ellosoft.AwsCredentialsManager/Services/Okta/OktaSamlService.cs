// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using AngleSharp.Html.Parser;
using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public record SamlData(string SamlAssertion, string SignInUrl, string RelayState);

public interface IOktaSamlService
{
    /// <summary>
    ///     Retrieves the SAML assertion for an Okta app using the session carried by the authentication result
    ///     (session token for classic authentication, session id for Identity Engine / FastPass authentication)
    /// </summary>
    Task<SamlData> GetAppSamlDataAsync(AuthenticationResult authenticationResult, string oktaAppUrl);
}

public class OktaSamlService(Func<HttpMessageHandler> httpMessageHandlerFactory) : IOktaSamlService
{
    public OktaSamlService() : this(() => new HttpClientHandler())
    {
    }

    public async Task<SamlData> GetAppSamlDataAsync(AuthenticationResult authenticationResult, string oktaAppUrl)
    {
        using var response = await GetAppPageAsync(authenticationResult, oktaAppUrl);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to retrieve SAML assertion. HTTP Status: {response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        var parser = new HtmlParser();
        using var document = await parser.ParseDocumentAsync(responseBody);

        var samlAssertion = document.QuerySelector("input[name=SAMLResponse]")?.GetAttribute("value")
                            ?? throw new InvalidOperationException(GetMissingSamlAssertionMessage(responseBody, response));

        var signInUrl = document.QuerySelector("form")?.GetAttribute("action")
                        ?? throw new InvalidOperationException("Sign-in URL not found in the Okta SAML response");

        return new SamlData
        (
            SamlAssertion: samlAssertion,
            SignInUrl: signInUrl,
            RelayState: document.QuerySelector("input[name=RelayState]")?.GetAttribute("value") ?? String.Empty
        );
    }

    private static string GetMissingSamlAssertionMessage(string responseBody, HttpResponseMessage response)
    {
        // Okta answered with its sign-in page: the session was not accepted for this app (e.g. the app sign-on policy requires re-verification)
        if (OktaLoginPageStateTokenExtractor.Extract(responseBody) is not null)
        {
            return "Okta requires additional verification to access this app (the Okta session was not accepted for the app sign-on policy). " +
                   $"Please try again. Okta responded at: {response.RequestMessage?.RequestUri}";
        }

        return $"SAML assertion not found in the Okta response. Please check the Okta app URL and try again (Okta responded at: {response.RequestMessage?.RequestUri})";
    }

    private Task<HttpResponseMessage> GetAppPageAsync(AuthenticationResult authenticationResult, string oktaAppUrl)
    {
        if (authenticationResult.SessionToken is not null)
            return RedirectUsingSessionCookie(authenticationResult.OktaDomain, oktaAppUrl, authenticationResult.SessionToken);

        if (authenticationResult.SessionCookies is not null)
            return GetUsingSessionCookies(oktaAppUrl, authenticationResult.SessionCookies);

        if (authenticationResult.SessionId is not null)
            return GetUsingSessionId(oktaAppUrl, authenticationResult.SessionId);

        throw new InvalidOperationException("Authentication result does not contain an Okta session");
    }

    private async Task<HttpResponseMessage> GetUsingSessionCookies(string oktaAppUrl, CookieContainer sessionCookies)
    {
        // Identity Engine sessions are carried by several cookies (sid, idx, device token...), replay the whole sign-in cookie jar
        var handler = httpMessageHandlerFactory();

        if (handler is HttpClientHandler clientHandler)
        {
            clientHandler.UseCookies = true;
            clientHandler.CookieContainer = sessionCookies;
        }

        using var httpClient = new HttpClient(handler);
        using var request = new HttpRequestMessage(HttpMethod.Get, oktaAppUrl);

        if (handler is not HttpClientHandler)
            request.Headers.Add("Cookie", sessionCookies.GetCookieHeader(new Uri(oktaAppUrl)));

        return await httpClient.SendAsync(request);
    }

    private async Task<HttpResponseMessage> RedirectUsingSessionCookie(Uri oktaDomain, string redirectUrl, string sessionToken)
    {
        // see: https://developer.okta.com/docs/guides/session-cookie/main/#retrieve-a-session-cookie-by-visiting-a-session-redirect-link
        const string OKTA_SESSION_REDIRECT_URL_TEMPLATE = "/login/sessionCookieRedirect?token=${sessionToken}&redirectUrl=${redirectUrl}";

        var sessionRedirectUrl = OKTA_SESSION_REDIRECT_URL_TEMPLATE
            .Replace("${sessionToken}", sessionToken)
            .Replace("${redirectUrl}", redirectUrl);

        using var httpClient = CreateHttpClient();

        return await httpClient.GetAsync(new Uri(oktaDomain, sessionRedirectUrl));
    }

    private async Task<HttpResponseMessage> GetUsingSessionId(string oktaAppUrl, string sessionId)
    {
        // see: https://developer.okta.com/docs/guides/session-cookie/main/#use-the-session-cookie
        using var httpClient = CreateHttpClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, oktaAppUrl);
        request.Headers.Add("Cookie", $"sid={sessionId}");

        return await httpClient.SendAsync(request);
    }

    private HttpClient CreateHttpClient() => new(httpMessageHandlerFactory());
}
