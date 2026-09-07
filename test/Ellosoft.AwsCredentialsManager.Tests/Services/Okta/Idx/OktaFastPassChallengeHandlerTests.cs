// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text.Json.Nodes;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Spectre.Console.Testing;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

public class OktaFastPassChallengeHandlerTests
{
    private const string OktaDomain = "https://xyz.okta.com/";
    private const string PollUrl = "https://xyz.okta.com/idp/idx/authenticators/poll";
    private const string CancelUrl = "https://xyz.okta.com/idp/idx/authenticators/poll/cancel";

    private readonly FakeHttpMessageHandler _oktaHandler = new();
    private readonly FakeHttpMessageHandler _loopbackHandler = new();
    private readonly IOktaVerifyAppLauncher _appLauncher = Substitute.For<IOktaVerifyAppLauncher>();
    private readonly TestConsole _console = new();
    private readonly OktaFastPassChallengeHandler _handler;
    private readonly IdxClient _idxClient;

    public OktaFastPassChallengeHandlerTests()
    {
        var httpClientFactory = Substitute.For<IOktaIdxHttpClientFactory>();
        httpClientFactory.CreateLoopbackClient().Returns(_ => new HttpClient(_loopbackHandler));

        _idxClient = new IdxClient(new HttpClient(_oktaHandler));

        _handler = new OktaFastPassChallengeHandler(httpClientFactory, _appLauncher, _console, NullLogger<OktaFastPassChallengeHandler>.Instance)
        {
            Timeout = TimeSpan.FromSeconds(5)
        };
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_ByDefault_ShouldAskOktaToOpenTheAppInsteadOfProbingLoopbackPorts()
    {
        // Okta answers the cancellation with the "launch-authenticator" remediation (Okta Verify deep link)
        _oktaHandler.OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.IdentifyWithoutPassword);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.HasRemediation("launch-authenticator").ShouldBeTrue();

        var body = JsonNode.Parse(_oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem().Body!)!;
        body["stateHandle"]!.GetValue<string>().ShouldBe(IdxPayloads.StateHandle);
        body["reason"]!.GetValue<string>().ShouldBe("OV_UNREACHABLE_BY_LOOPBACK");

        _loopbackHandler.Requests.ShouldBeEmpty();
        _oktaHandler.RequestsTo(HttpMethod.Post, PollUrl).ShouldBeEmpty();
        _console.Output.ShouldContain("Okta Verify");
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_ByDefault_WhenOktaDoesNotOfferToOpenTheApp_ShouldFallBackToLoopback()
    {
        _loopbackHandler
            .OnStatus(HttpMethod.Get, "http://localhost:8769/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:8769/challenge", HttpStatusCode.OK);

        _oktaHandler
            .OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.LoopbackChallenge())
            .OnJson(HttpMethod.Post, PollUrl, IdxPayloads.LoopbackChallenge(), IdxPayloads.Success);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem();
        _loopbackHandler.RequestsTo(HttpMethod.Post, "http://localhost:8769/challenge").ShouldHaveSingleItem();
        _appLauncher.DidNotReceiveWithAnyArgs().TryLaunch(default!);
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_ShouldProbePortsAndPostChallengeToFirstListeningPort()
    {
        _handler.PreferAppLaunch = false;

        _loopbackHandler
            .OnUnreachable(HttpMethod.Get, "http://localhost:8769/probe")
            .OnStatus(HttpMethod.Get, "http://localhost:65111/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:65111/challenge", HttpStatusCode.OK);

        _oktaHandler.OnJson(HttpMethod.Post, PollUrl, IdxPayloads.LoopbackChallenge(), IdxPayloads.Success);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var challengeRequest = _loopbackHandler.RequestsTo(HttpMethod.Post, "http://localhost:65111/challenge").ShouldHaveSingleItem();
        JsonNode.Parse(challengeRequest.Body!)!["challengeRequest"]!.GetValue<string>().ShouldBe("eyJraWQ.challenge.jwt");
        challengeRequest.Request.Headers.GetValues("Origin").ShouldBe(["https://xyz.okta.com"]);

        _loopbackHandler.RequestsTo(HttpMethod.Post, "http://localhost:8769/challenge").ShouldBeEmpty();
        _oktaHandler.RequestsTo(HttpMethod.Post, PollUrl).ShouldAllBe(r => JsonNode.Parse(r.Body!)!["stateHandle"]!.GetValue<string>() == IdxPayloads.StateHandle);
        _appLauncher.DidNotReceiveWithAnyArgs().TryLaunch(default!);
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_WhenChallengeReturns503_ShouldTryNextPort()
    {
        _handler.PreferAppLaunch = false;

        _loopbackHandler
            .OnStatus(HttpMethod.Get, "http://localhost:8769/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:8769/challenge", HttpStatusCode.ServiceUnavailable)
            .OnStatus(HttpMethod.Get, "http://localhost:65111/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:65111/challenge", HttpStatusCode.OK);

        _oktaHandler.OnJson(HttpMethod.Post, PollUrl, IdxPayloads.Success);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _loopbackHandler.RequestsTo(HttpMethod.Post, "http://localhost:65111/challenge").ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_WhenNoPortIsListening_ShouldCancelPollingWithUnreachableReason()
    {
        _handler.PreferAppLaunch = false;

        _loopbackHandler
            .OnUnreachable(HttpMethod.Get, "http://localhost:8769/probe")
            .OnUnreachable(HttpMethod.Get, "http://localhost:65111/probe");

        _oktaHandler
            .OnJson(HttpMethod.Post, PollUrl, IdxPayloads.LoopbackChallenge())
            .OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.IdentifyWithoutPassword);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.HasRemediation("launch-authenticator").ShouldBeTrue();

        var cancel = _oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem();
        var body = JsonNode.Parse(cancel.Body!)!;
        body["stateHandle"]!.GetValue<string>().ShouldBe(IdxPayloads.StateHandle);
        body["reason"]!.GetValue<string>().ShouldBe("OV_UNREACHABLE_BY_LOOPBACK");
        body["statusCode"].ShouldBeNull();
    }

    [Theory]
    [InlineData("http://evil.example.com")]
    [InlineData("https://localhost")]
    [InlineData("not a url")]
    public async Task ExecuteAsync_Loopback_WhenChallengeDomainIsNotHttpLoopback_ShouldNotContactItAndCancelPolling(string domain)
    {
        _handler.PreferAppLaunch = false;

        var payload = IdxPayloads.LoopbackChallenge().Replace("\"domain\": \"http://localhost\"", $"\"domain\": \"{domain}\"");

        _oktaHandler
            .OnJson(HttpMethod.Post, PollUrl, payload)
            .OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.IdentifyWithoutPassword);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(payload), CancellationToken.None);

        result.HasRemediation("launch-authenticator").ShouldBeTrue();
        _loopbackHandler.Requests.ShouldBeEmpty();
        JsonNode.Parse(_oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem().Body!)!["reason"]!.GetValue<string>().ShouldBe("OV_UNREACHABLE_BY_LOOPBACK");
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_WhenOktaVerifyReturnsError_ShouldCancelPollingWithErrorReason()
    {
        _handler.PreferAppLaunch = false;

        _loopbackHandler
            .OnStatus(HttpMethod.Get, "http://localhost:8769/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:8769/challenge", HttpStatusCode.BadRequest);

        _oktaHandler
            .OnJson(HttpMethod.Post, PollUrl, IdxPayloads.LoopbackChallenge())
            .OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.SelectAuthenticator);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge()), CancellationToken.None);

        result.HasRemediation("select-authenticator-authenticate").ShouldBeTrue();

        var body = JsonNode.Parse(_oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem().Body!)!;
        body["reason"]!.GetValue<string>().ShouldBe("OV_RETURNED_ERROR");
        body["statusCode"]!.GetValue<int>().ShouldBe(400);
        _loopbackHandler.RequestsTo(HttpMethod.Get, "http://localhost:65111/probe").ShouldBeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_Loopback_WhenOktaIssuesSecondChallenge_ShouldSendNewChallengeRequest()
    {
        _handler.PreferAppLaunch = false;

        _loopbackHandler
            .OnStatus(HttpMethod.Get, "http://localhost:8769/probe", HttpStatusCode.OK)
            .OnStatus(HttpMethod.Post, "http://localhost:8769/challenge", HttpStatusCode.OK);

        _oktaHandler.OnJson(HttpMethod.Post, PollUrl,
            IdxPayloads.LoopbackChallenge("eyJraWQ.second.jwt"),
            IdxPayloads.LoopbackChallenge("eyJraWQ.second.jwt"),
            IdxPayloads.Success);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.LoopbackChallenge("eyJraWQ.first.jwt")), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();

        var challenges = _loopbackHandler.RequestsTo(HttpMethod.Post, "http://localhost:8769/challenge")
            .Select(r => JsonNode.Parse(r.Body!)!["challengeRequest"]!.GetValue<string>())
            .ToList();

        challenges.ShouldBe(["eyJraWQ.first.jwt", "eyJraWQ.second.jwt"]);
    }

    [Fact]
    public async Task ExecuteAsync_CustomUri_ShouldLaunchOktaVerifyAndPollUntilSuccess()
    {
        _appLauncher.TryLaunch(Arg.Any<string>()).Returns(true);
        _oktaHandler.OnJson(HttpMethod.Post, PollUrl, IdxPayloads.CustomUriChallenge(), IdxPayloads.Success);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.CustomUriChallenge()), CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        _appLauncher.Received(1).TryLaunch("com-okta-authenticator:/deviceChallenge?challengeRequest=eyJraWQ.custom.jwt");
        _loopbackHandler.Requests.ShouldBeEmpty();
        _oktaHandler.RequestsTo(HttpMethod.Post, PollUrl).Count().ShouldBe(2);
    }

    [Fact]
    public async Task ExecuteAsync_CustomUri_WhenAppCannotBeLaunched_ShouldThrow()
    {
        _appLauncher.TryLaunch(Arg.Any<string>()).Returns(false);
        _oktaHandler.OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.SelectAuthenticator);

        var act = () => _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.CustomUriChallenge()), CancellationToken.None);

        var exception = await Should.ThrowAsync<OktaFastPassException>(act);
        exception.Message.ShouldContain("Okta Verify");
        _oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem();
    }

    [Fact]
    public async Task ExecuteAsync_WhenPollReturnsError_ShouldReturnErrorResponse()
    {
        _appLauncher.TryLaunch(Arg.Any<string>()).Returns(true);
        _oktaHandler.OnJson(HttpMethod.Post, PollUrl, IdxPayloads.InvalidCredentialsError);

        var result = await _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.CustomUriChallenge()), CancellationToken.None);

        result.HasErrors.ShouldBeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WhenUserNeverApproves_ShouldCancelPollingAndThrowTimeout()
    {
        _handler.Timeout = TimeSpan.FromMilliseconds(150);
        _appLauncher.TryLaunch(Arg.Any<string>()).Returns(true);

        _oktaHandler
            .OnJson(HttpMethod.Post, PollUrl, IdxPayloads.CustomUriChallenge())
            .OnJson(HttpMethod.Post, CancelUrl, IdxPayloads.SelectAuthenticator);

        var act = () => _handler.ExecuteAsync(_idxClient, new Uri(OktaDomain), IdxResponse.Parse(IdxPayloads.CustomUriChallenge()), CancellationToken.None);

        var exception = await Should.ThrowAsync<OktaFastPassException>(act);
        exception.Message.ShouldContain("Timed out");

        var body = JsonNode.Parse(_oktaHandler.RequestsTo(HttpMethod.Post, CancelUrl).ShouldHaveSingleItem().Body!)!;
        body["reason"]!.GetValue<string>().ShouldBe("USER_CANCELED");
    }
}
