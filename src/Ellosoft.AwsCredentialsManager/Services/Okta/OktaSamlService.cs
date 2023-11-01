// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using AngleSharp.Html.Parser;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public record SamlData(string SamlAssertion, string SignInUrl, string RelayState);

public class OktaSamlService
{
    public async Task<SamlData> GetAppSamlDataAsync(Uri oktaDomain, string oktaAppUrl, string sessionToken)
    {
        using var response = await RedirectUsingSessionCookie(oktaDomain, oktaAppUrl, sessionToken);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to retrieve SAML assertion. HTTP Status: {response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        var parser = new HtmlParser();
        using var document = parser.ParseDocument(responseBody);

        return new SamlData
        (
            SamlAssertion: document.QuerySelector("input[name=SAMLResponse]")?.GetAttribute("value")!,
            SignInUrl: document.QuerySelector("form")?.GetAttribute("action")!,
            RelayState: document.QuerySelector("input[name=RelayState]")?.GetAttribute("value") ?? String.Empty
        );
    }

    private static async Task<HttpResponseMessage> RedirectUsingSessionCookie(Uri oktaDomain, string redirectUrl, string sessionToken)
    {
        // see: https://developer.okta.com/docs/guides/session-cookie/main/#retrieve-a-session-cookie-by-visiting-a-session-redirect-link
        const string OKTA_SESSION_REDIRECT_URL_TEMPLATE = "/login/sessionCookieRedirect?token=${sessionToken}&redirectUrl=${redirectUrl}";

        var sessionRedirectUrl = OKTA_SESSION_REDIRECT_URL_TEMPLATE
            .Replace("${sessionToken}", sessionToken)
            .Replace("${redirectUrl}", redirectUrl);

        using var httpClient = new HttpClient();

        return await httpClient.GetAsync(new Uri(oktaDomain, sessionRedirectUrl));
    }
}
