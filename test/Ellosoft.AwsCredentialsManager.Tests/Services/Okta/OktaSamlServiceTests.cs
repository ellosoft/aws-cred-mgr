// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta;

public class OktaSamlServiceTests
{
    private const string OktaAppUrl = "https://xyz.okta.com/home/amazon_aws/abc/272";
    private static readonly Uri OktaDomain = new("https://xyz.okta.com/");

    private const string SamlPage =
        """
        <html><body>
          <form action="https://signin.aws.amazon.com/saml" method="post">
            <input name="SAMLResponse" type="hidden" value="c2FtbC1hc3NlcnRpb24="/>
            <input name="RelayState" type="hidden" value="relay"/>
          </form>
        </body></html>
        """;

    private readonly FakeHttpMessageHandler _handler = new();
    private readonly OktaSamlService _samlService;

    public OktaSamlServiceTests()
    {
        _samlService = new OktaSamlService(() => _handler);
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WithSessionToken_ShouldUseSessionCookieRedirect()
    {
        var redirectUrl = $"https://xyz.okta.com/login/sessionCookieRedirect?token=session-token&redirectUrl={OktaAppUrl}";
        _handler.On(HttpMethod.Get, redirectUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html(SamlPage)));

        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = true, SessionToken = "session-token" };

        var samlData = await _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl);

        samlData.SamlAssertion.ShouldBe("c2FtbC1hc3NlcnRpb24=");
        samlData.SignInUrl.ShouldBe("https://signin.aws.amazon.com/saml");
        samlData.RelayState.ShouldBe("relay");
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WithSessionIdOnly_ShouldRequestAppUrlWithSessionCookie()
    {
        _handler.On(HttpMethod.Get, OktaAppUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html(SamlPage)));

        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = true, SessionId = "102sid" };

        var samlData = await _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl);

        samlData.SamlAssertion.ShouldBe("c2FtbC1hc3NlcnRpb24=");

        var request = _handler.RequestsTo(HttpMethod.Get, OktaAppUrl).ShouldHaveSingleItem();
        request.Request.Headers.GetValues("Cookie").ShouldContain(c => c.Contains("sid=102sid"));
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WithSessionCookies_ShouldSendAllSignInCookiesToAppUrl()
    {
        // Identity Engine sessions are carried by several cookies (sid, idx, DT...), not only the session id
        var cookies = new CookieContainer();
        cookies.Add(OktaDomain, new Cookie("sid", "102sid"));
        cookies.Add(OktaDomain, new Cookie("idx", "eyJ-idx-session"));
        cookies.Add(OktaDomain, new Cookie("DT", "device-token"));

        _handler.On(HttpMethod.Get, OktaAppUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html(SamlPage)));

        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = true, SessionId = "102sid", SessionCookies = cookies };

        var samlData = await _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl);

        samlData.SamlAssertion.ShouldBe("c2FtbC1hc3NlcnRpb24=");

        var request = _handler.RequestsTo(HttpMethod.Get, OktaAppUrl).ShouldHaveSingleItem();
        var cookieHeader = string.Join("; ", request.Request.Headers.GetValues("Cookie"));

        cookieHeader.ShouldContain("sid=102sid");
        cookieHeader.ShouldContain("idx=eyJ-idx-session");
        cookieHeader.ShouldContain("DT=device-token");
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WhenOktaReturnsSignInPageInsteadOfSaml_ShouldExplainAdditionalVerification()
    {
        const string SIGN_IN_PAGE = "<html><script>var stateToken = '02app\\x2Dstep\\x2Dup';</script></html>";
        _handler.On(HttpMethod.Get, OktaAppUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html(SIGN_IN_PAGE)));

        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = true, SessionId = "102sid" };

        var exception = await Should.ThrowAsync<InvalidOperationException>(() => _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl));

        exception.Message.ShouldContain("additional verification");
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WithoutSession_ShouldThrow()
    {
        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = false };

        await Should.ThrowAsync<InvalidOperationException>(() => _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl));
    }

    [Fact]
    public async Task GetAppSamlDataAsync_WhenOktaReturnsError_ShouldThrow()
    {
        _handler.OnStatus(HttpMethod.Get, OktaAppUrl, HttpStatusCode.Forbidden);

        var authResult = new AuthenticationResult { OktaDomain = OktaDomain, Authenticated = true, SessionId = "102sid" };

        await Should.ThrowAsync<InvalidOperationException>(() => _samlService.GetAppSamlDataAsync(authResult, OktaAppUrl));
    }
}
