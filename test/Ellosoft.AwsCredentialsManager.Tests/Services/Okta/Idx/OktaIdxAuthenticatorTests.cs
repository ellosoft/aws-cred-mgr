// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text.Json.Nodes;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Spectre.Console.Testing;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

public class OktaIdxAuthenticatorTests
{
    private const string OktaDomain = "https://xyz.okta.com/";
    private const string IntrospectUrl = "https://xyz.okta.com/idp/idx/introspect";
    private const string IdentifyUrl = "https://xyz.okta.com/idp/idx/identify";
    private const string SelectAuthenticatorUrl = "https://xyz.okta.com/idp/idx/challenge";
    private const string AnswerUrl = "https://xyz.okta.com/idp/idx/challenge/answer";
    private const string LaunchUrl = "https://xyz.okta.com/idp/idx/authenticators/okta-verify/launch";
    private const string SuccessRedirectUrl = "https://xyz.okta.com/login/token/redirect?stateToken=02state-handle";
    private const string SessionsMeUrl = "https://xyz.okta.com/api/v1/sessions/me";
    private const string SignInPageUrlPrefix = "https://xyz.okta.com/oauth2/v1/authorize?";

    private const string LoginPage = """<html><script>var stateToken = '02state\x2Dtoken';</script></html>""";
    private const string DashboardShell = """<html><head><meta name="ui-service" content="iris"/></head><body><div id="root"></div></body></html>""";
    private const string SessionsMe = """{ "id": "102sid", "userId": "00u1", "login": "john@xyz.com", "status": "ACTIVE" }""";

    private readonly FakeHttpMessageHandler _oktaHandler = new();
    private readonly IOktaFastPassChallengeHandler _challengeHandler = Substitute.For<IOktaFastPassChallengeHandler>();
    private readonly TestConsole _console = new();
    private readonly OktaIdxAuthenticator _authenticator;

    private CookieContainer? _sessionCookies;

    public OktaIdxAuthenticatorTests()
    {
        var httpClientFactory = Substitute.For<IOktaIdxHttpClientFactory>();
        httpClientFactory.CreateSessionClient(Arg.Do<CookieContainer>(cookies => _sessionCookies = cookies)).Returns(_ => new HttpClient(_oktaHandler));

        // Identity Engine orgs serve the End-User Dashboard SPA on the org root, the sign-in page is rendered by the OIDC authorize endpoint
        _oktaHandler
            .OnPrefix(HttpMethod.Get, SignInPageUrlPrefix, _ => Task.FromResult(FakeHttpMessageHandler.Html(LoginPage)))
            .On(HttpMethod.Get, OktaDomain, _ => Task.FromResult(FakeHttpMessageHandler.Html(DashboardShell)))
            .On(HttpMethod.Get, SuccessRedirectUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html("<html>Okta Dashboard</html>")))
            .OnJson(HttpMethod.Get, SessionsMeUrl, SessionsMe);

        _authenticator = new OktaIdxAuthenticator(httpClientFactory, _challengeHandler, _console, NullLogger<OktaIdxAuthenticator>.Instance);
    }

    [Fact]
    public async Task AuthenticateAsync_ShouldBootstrapTransactionFromOktaDashboardAuthorizeRequest()
    {
        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback);

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.Success));

        await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        var signInPageRequest = _oktaHandler.RequestsToPrefix(HttpMethod.Get, SignInPageUrlPrefix).ShouldHaveSingleItem();
        var query = System.Web.HttpUtility.ParseQueryString(new Uri(signInPageRequest.Url).Query);

        query["client_id"].ShouldBe(OktaIdxAuthenticator.OktaDashboardClientId);
        query["redirect_uri"].ShouldBe("https://xyz.okta.com/enduser/callback");
        query["response_type"].ShouldBe("code");
        query["scope"].ShouldBe("openid profile email okta.users.read.self");
        query["code_challenge_method"].ShouldBe("S256");
        query["code_challenge"].ShouldNotBeNullOrWhiteSpace();
        query["state"].ShouldNotBeNullOrWhiteSpace();
        query["nonce"].ShouldNotBeNullOrWhiteSpace();

        // the sign-in page had a state token, no need to fall back to the org root
        _oktaHandler.RequestsTo(HttpMethod.Get, OktaDomain).ShouldBeEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenAuthorizePageHasNoStateToken_ShouldFallBackToOrgRootSignInPage()
    {
        var handler = new FakeHttpMessageHandler()
            .OnPrefix(HttpMethod.Get, SignInPageUrlPrefix, _ => Task.FromResult(FakeHttpMessageHandler.Html(DashboardShell)))
            .On(HttpMethod.Get, OktaDomain, _ => Task.FromResult(FakeHttpMessageHandler.Html(LoginPage)))
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback)
            .On(HttpMethod.Get, SuccessRedirectUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html("<html/>")))
            .OnJson(HttpMethod.Get, SessionsMeUrl, SessionsMe);

        var httpClientFactory = Substitute.For<IOktaIdxHttpClientFactory>();
        httpClientFactory.CreateSessionClient(Arg.Any<CookieContainer>()).Returns(_ => new HttpClient(handler));

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.Success));

        var authenticator = new OktaIdxAuthenticator(httpClientFactory, _challengeHandler, _console, NullLogger<OktaIdxAuthenticator>.Instance);

        var result = await authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.SessionId.ShouldBe("102sid");
        handler.RequestsToPrefix(HttpMethod.Get, SignInPageUrlPrefix).ShouldHaveSingleItem();
        handler.RequestsTo(HttpMethod.Get, OktaDomain).ShouldHaveSingleItem();
        Body(handler.RequestsTo(HttpMethod.Post, IntrospectUrl).ShouldHaveSingleItem())["stateToken"]!.GetValue<string>().ShouldBe("02state-token");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenIdentifyAsksForPassword_ShouldIdentifyRunFastPassAndReturnSessionId()
    {
        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback);

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Is<IdxResponse>(r => r.PollRemediation != null), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.Success));

        var result = await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.Authenticated.ShouldBeTrue();
        result.SessionId.ShouldBe("102sid");
        result.SessionToken.ShouldBeNull();
        result.MfaUsed.ShouldBe(OktaMfaFactorSelector.FastPassFactorCode);
        result.OktaDomain.ShouldBe(new Uri(OktaDomain));

        // the cookies Okta set during the sign-in (sid, idx, DT...) are the session, they must travel with the result
        result.SessionCookies.ShouldNotBeNull().ShouldBeSameAs(_sessionCookies);

        var introspect = Body(_oktaHandler.RequestsTo(HttpMethod.Post, IntrospectUrl).ShouldHaveSingleItem());
        introspect["stateToken"]!.GetValue<string>().ShouldBe("02state-token");

        var identify = Body(_oktaHandler.RequestsTo(HttpMethod.Post, IdentifyUrl).ShouldHaveSingleItem());
        identify["identifier"]!.GetValue<string>().ShouldBe("john@xyz.com");
        identify["credentials"]!["passcode"]!.GetValue<string>().ShouldBe("P@ssw0rd");
        identify["stateHandle"]!.GetValue<string>().ShouldBe(IdxPayloads.StateHandle);

        _oktaHandler.RequestsTo(HttpMethod.Get, SuccessRedirectUrl).ShouldHaveSingleItem();
        _oktaHandler.RequestsTo(HttpMethod.Get, SessionsMeUrl).ShouldHaveSingleItem();

        _console.Output.ShouldContain("Authenticated!");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenPasswordIsASeparateStep_ShouldSelectPasswordFirstThenOktaVerifyFastPass()
    {
        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithoutPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.SelectAuthenticator)
            .OnJson(HttpMethod.Post, SelectAuthenticatorUrl, IdxPayloads.ChallengePasswordAuthenticator, IdxPayloads.ChallengePollLoopback)
            .OnJson(HttpMethod.Post, AnswerUrl, IdxPayloads.SelectAuthenticator);

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.Success));

        var result = await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.Authenticated.ShouldBeTrue();
        result.SessionId.ShouldBe("102sid");

        var identify = Body(_oktaHandler.RequestsTo(HttpMethod.Post, IdentifyUrl).ShouldHaveSingleItem());
        identify["identifier"]!.GetValue<string>().ShouldBe("john@xyz.com");
        identify["credentials"].ShouldBeNull();

        var selections = _oktaHandler.RequestsTo(HttpMethod.Post, SelectAuthenticatorUrl).Select(Body).ToList();
        selections.Count.ShouldBe(2);
        selections[0]["authenticator"]!["id"]!.GetValue<string>().ShouldBe("aut-password");
        selections[1]["authenticator"]!["id"]!.GetValue<string>().ShouldBe("aut-okta-verify");
        selections[1]["authenticator"]!["methodType"]!.GetValue<string>().ShouldBe("signed_nonce");

        var answer = Body(_oktaHandler.RequestsTo(HttpMethod.Post, AnswerUrl).ShouldHaveSingleItem());
        answer["credentials"]!["passcode"]!.GetValue<string>().ShouldBe("P@ssw0rd");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenLoopbackFallsBackToLaunchAuthenticator_ShouldLaunchAndRunChallengeAgain()
    {
        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback)
            .OnJson(HttpMethod.Post, LaunchUrl, IdxPayloads.DeviceChallengePollCustomUri);

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.IdentifyWithoutPassword), IdxResponse.Parse(IdxPayloads.Success));

        var result = await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.Authenticated.ShouldBeTrue();

        Body(_oktaHandler.RequestsTo(HttpMethod.Post, LaunchUrl).ShouldHaveSingleItem())["stateHandle"]!.GetValue<string>().ShouldBe(IdxPayloads.StateHandle);
        _oktaHandler.RequestsTo(HttpMethod.Post, IdentifyUrl).ShouldHaveSingleItem();

        await _challengeHandler.Received(2).ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AuthenticateAsync_WhenOktaOffersLaunchAuthenticatorNextToSelectAuthenticator_ShouldOpenTheAppInsteadOfReselectingOktaVerify()
    {
        // real orgs answer the loopback cancellation with both "launch-authenticator" and "select-authenticator-authenticate" (verify with something else)
        var launchRemediation = JsonNode.Parse(
            """
            {
              "rel": ["create-form"],
              "name": "launch-authenticator",
              "href": "https://xyz.okta.com/idp/idx/authenticators/okta-verify/launch",
              "method": "POST",
              "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
            }
            """);

        var launchAndSelectNode = JsonNode.Parse(IdxPayloads.SelectAuthenticator)!;
        launchAndSelectNode["remediation"]!["value"]!.AsArray().Insert(0, launchRemediation);
        var launchAndSelect = launchAndSelectNode.ToJsonString();

        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback)
            .OnJson(HttpMethod.Post, LaunchUrl, IdxPayloads.DeviceChallengePollCustomUri);

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(launchAndSelect), IdxResponse.Parse(IdxPayloads.Success));

        var result = await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.Authenticated.ShouldBeTrue();
        _oktaHandler.RequestsTo(HttpMethod.Post, LaunchUrl).ShouldHaveSingleItem();
        _oktaHandler.RequestsTo(HttpMethod.Post, SelectAuthenticatorUrl).ShouldBeEmpty();
    }

    [Fact]
    public async Task AuthenticateAsync_WhenFastPassIsNotOffered_ShouldThrowListingOfferedAuthenticators()
    {
        var withoutFastPass = IdxPayloads.SelectAuthenticator.Replace("""{ "value": "signed_nonce", "label": "Use Okta FastPass" },""", string.Empty);

        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, withoutFastPass);

        var exception = await Should.ThrowAsync<OktaFastPassException>(() =>
            _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None));

        exception.Message.ShouldContain("FastPass");
        exception.Message.ShouldContain("Okta Verify");
        exception.Message.ShouldContain("Password");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenCredentialsAreInvalid_ShouldThrowInvalidUsernameOrPasswordException()
    {
        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .On(HttpMethod.Post, IdentifyUrl, _ => Task.FromResult(FakeHttpMessageHandler.Json(IdxPayloads.InvalidCredentialsError, System.Net.HttpStatusCode.Unauthorized)));

        await Should.ThrowAsync<InvalidUsernameOrPasswordException>(() =>
            _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "wrong", CancellationToken.None));
    }

    [Fact]
    public async Task AuthenticateAsync_WhenOktaReturnsOtherErrors_ShouldReturnUnauthenticatedResultAndPrintMessage()
    {
        const string lockedOut =
            """
            { "version": "1.0.0", "stateHandle": "02state-handle",
              "messages": { "type": "array", "value": [ { "message": "Your account is locked", "i18n": { "key": "errors.E0000005" }, "class": "ERROR" } ] } }
            """;

        _oktaHandler
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, lockedOut);

        var result = await _authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None);

        result.Authenticated.ShouldBeFalse();
        result.SessionId.ShouldBeNull();
        _console.Output.ShouldContain("Your account is locked");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenOrgIsNotOnIdentityEngine_ShouldThrowOktaFastPassException()
    {
        // Classic Engine renders the sign-in widget with an empty state token on both pages
        var handler = new FakeHttpMessageHandler()
            .OnPrefix(HttpMethod.Get, SignInPageUrlPrefix, _ => Task.FromResult(FakeHttpMessageHandler.Html("<html><script>var stateToken = '';</script></html>")))
            .On(HttpMethod.Get, OktaDomain, _ => Task.FromResult(FakeHttpMessageHandler.Html("<html><script>var stateToken = '';</script></html>")));

        var httpClientFactory = Substitute.For<IOktaIdxHttpClientFactory>();
        httpClientFactory.CreateSessionClient(Arg.Any<CookieContainer>()).Returns(_ => new HttpClient(handler));

        var authenticator = new OktaIdxAuthenticator(httpClientFactory, _challengeHandler, _console, NullLogger<OktaIdxAuthenticator>.Instance);

        var exception = await Should.ThrowAsync<OktaFastPassException>(() =>
            authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None));

        exception.Message.ShouldContain("Identity Engine");
    }

    [Fact]
    public async Task AuthenticateAsync_WhenSessionCannotBeRetrieved_ShouldThrowOktaFastPassException()
    {
        var handler = new FakeHttpMessageHandler()
            .OnPrefix(HttpMethod.Get, SignInPageUrlPrefix, _ => Task.FromResult(FakeHttpMessageHandler.Html(LoginPage)))
            .OnJson(HttpMethod.Post, IntrospectUrl, IdxPayloads.IdentifyWithPassword)
            .OnJson(HttpMethod.Post, IdentifyUrl, IdxPayloads.ChallengePollLoopback)
            .On(HttpMethod.Get, SuccessRedirectUrl, _ => Task.FromResult(FakeHttpMessageHandler.Html("<html/>")))
            .OnStatus(HttpMethod.Get, SessionsMeUrl, System.Net.HttpStatusCode.NotFound);

        var httpClientFactory = Substitute.For<IOktaIdxHttpClientFactory>();
        httpClientFactory.CreateSessionClient(Arg.Any<CookieContainer>()).Returns(_ => new HttpClient(handler));

        _challengeHandler
            .ExecuteAsync(Arg.Any<IdxClient>(), Arg.Any<Uri>(), Arg.Any<IdxResponse>(), Arg.Any<CancellationToken>())
            .Returns(IdxResponse.Parse(IdxPayloads.Success));

        var authenticator = new OktaIdxAuthenticator(httpClientFactory, _challengeHandler, _console, NullLogger<OktaIdxAuthenticator>.Instance);

        await Should.ThrowAsync<OktaFastPassException>(() =>
            authenticator.AuthenticateAsync(new Uri(OktaDomain), "john@xyz.com", "P@ssw0rd", CancellationToken.None));
    }

    private static JsonNode Body(FakeHttpMessageHandler.RecordedRequest request) => JsonNode.Parse(request.Body!)!;
}
