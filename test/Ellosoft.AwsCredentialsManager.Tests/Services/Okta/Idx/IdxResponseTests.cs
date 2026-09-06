// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

public class IdxResponseTests
{
    [Fact]
    public void Parse_ShouldExposeStateHandleAndRemediations()
    {
        var response = IdxResponse.Parse(IdxPayloads.IdentifyWithPassword);

        response.StateHandle.ShouldBe("02state-handle");
        response.IsSuccess.ShouldBeFalse();
        response.RemediationNames.ShouldBe(["identify", "select-enroll-profile"]);
        response.HasRemediation("identify").ShouldBeTrue();
        response.GetRemediation("identify")!.Href.ShouldBe("https://xyz.okta.com/idp/idx/identify");
        response.GetRemediation("does-not-exist").ShouldBeNull();
    }

    [Fact]
    public void IdentifyRequiresPassword_WhenIdentifyFormHasCredentials_ShouldBeTrue()
    {
        IdxResponse.Parse(IdxPayloads.IdentifyWithPassword).IdentifyRequiresPassword.ShouldBeTrue();
        IdxResponse.Parse(IdxPayloads.IdentifyWithoutPassword).IdentifyRequiresPassword.ShouldBeFalse();
    }

    [Fact]
    public void Success_ShouldExposeSuccessHref()
    {
        var response = IdxResponse.Parse(IdxPayloads.Success);

        response.IsSuccess.ShouldBeTrue();
        response.SuccessHref.ShouldBe("https://xyz.okta.com/login/token/redirect?stateToken=02state-handle");
    }

    [Fact]
    public void ErrorMessages_ShouldExposeMessageAndKey()
    {
        var response = IdxResponse.Parse(IdxPayloads.InvalidCredentialsError);

        response.HasErrors.ShouldBeTrue();
        response.ErrorMessages.Count.ShouldBe(1);
        response.ErrorMessages[0].Message.ShouldBe("Authentication failed");
        response.ErrorMessages[0].Key.ShouldBe("errors.E0000004");
    }

    [Fact]
    public void ErrorMessages_WhenNoMessages_ShouldBeEmpty()
    {
        var response = IdxResponse.Parse(IdxPayloads.IdentifyWithPassword);

        response.HasErrors.ShouldBeFalse();
        response.ErrorMessages.ShouldBeEmpty();
    }

    [Fact]
    public void DeviceChallenge_WhenChallengePollWithLoopback_ShouldExposeLoopbackDetails()
    {
        var response = IdxResponse.Parse(IdxPayloads.ChallengePollLoopback);

        var challenge = response.DeviceChallenge.ShouldNotBeNull();
        challenge.Method.ShouldBe("LOOPBACK");
        challenge.ChallengeRequest.ShouldBe("eyJraWQ.challenge.jwt");
        challenge.Domain.ShouldBe("http://localhost");
        challenge.Ports.ShouldBe(["8769", "65111", "65121"]);
        challenge.ProbeTimeoutMillis.ShouldBe(3000);
        challenge.Href.ShouldBeNull();

        var pollRemediation = response.PollRemediation.ShouldNotBeNull();
        pollRemediation.Href.ShouldBe("https://xyz.okta.com/idp/idx/authenticators/poll");
        pollRemediation.Refresh.ShouldBe(4000);
        response.CancelPollingHref.ShouldBe("https://xyz.okta.com/idp/idx/authenticators/poll/cancel");
    }

    [Fact]
    public void DeviceChallenge_WhenDeviceChallengePollWithCustomUri_ShouldExposeHref()
    {
        var response = IdxResponse.Parse(IdxPayloads.DeviceChallengePollCustomUri);

        var challenge = response.DeviceChallenge.ShouldNotBeNull();
        challenge.Method.ShouldBe("CUSTOM_URI");
        challenge.Href.ShouldBe("com-okta-authenticator:/deviceChallenge?challengeRequest=eyJraWQ.custom.jwt");
        challenge.Ports.ShouldBeEmpty();

        response.PollRemediation.ShouldNotBeNull().Refresh.ShouldBe(2000);
        response.CancelPollingHref.ShouldBe("https://xyz.okta.com/idp/idx/authenticators/poll/cancel");
    }

    [Fact]
    public void DeviceChallenge_WhenNoChallenge_ShouldBeNull()
    {
        var response = IdxResponse.Parse(IdxPayloads.SelectAuthenticator);

        response.DeviceChallenge.ShouldBeNull();
        response.PollRemediation.ShouldBeNull();
    }

    [Fact]
    public void CurrentAuthenticatorKey_ShouldReadCurrentAuthenticator()
    {
        IdxResponse.Parse(IdxPayloads.ChallengePasswordAuthenticator).CurrentAuthenticatorKey.ShouldBe("okta_password");
        IdxResponse.Parse(IdxPayloads.ChallengePollLoopback).CurrentAuthenticatorKey.ShouldBe("okta_verify");
        IdxResponse.Parse(IdxPayloads.IdentifyWithPassword).CurrentAuthenticatorKey.ShouldBeNull();
    }

    [Fact]
    public void GetAuthenticatorOption_ShouldResolveOptionsByKeyUsingRelatesTo()
    {
        var response = IdxResponse.Parse(IdxPayloads.SelectAuthenticator);

        var oktaVerify = response.GetAuthenticatorOption("okta_verify").ShouldNotBeNull();
        oktaVerify.Id.ShouldBe("aut-okta-verify");
        oktaVerify.MethodTypes.ShouldBe(["signed_nonce", "push", "totp"]);

        var password = response.GetAuthenticatorOption("okta_password").ShouldNotBeNull();
        password.Id.ShouldBe("aut-password");
        password.MethodTypes.ShouldBe(["password"]);

        response.GetAuthenticatorOption("webauthn").ShouldBeNull();
        response.AuthenticatorOptionLabels.ShouldBe(["Okta Verify", "Password"]);
    }
}
