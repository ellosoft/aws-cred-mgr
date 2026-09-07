// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;
using Microsoft.Extensions.Logging.Abstractions;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

public class OktaVerifyAppLauncherTests
{
    private readonly OktaVerifyAppLauncher _launcher = new(NullLogger<OktaVerifyAppLauncher>.Instance);

    [Theory]
    [InlineData("https://evil.example.com/")]
    [InlineData("file:///etc/passwd")]
    [InlineData("javascript:alert(1)")]
    [InlineData("com-okta-authenticator-evil:/deviceChallenge")]
    [InlineData("not a uri")]
    [InlineData("")]
    public void TryLaunch_WhenUriIsNotAnOktaVerifyDeepLink_ShouldRefuseToHandItToTheOs(string uri)
    {
        // the URI comes from the IDX response, only the Okta Verify scheme may be handed to the OS URI handler
        _launcher.TryLaunch(uri).ShouldBeFalse();
    }

    [Theory]
    [InlineData("com-okta-authenticator:/deviceChallenge?challengeRequest=eyJraWQ.jwt")]
    [InlineData("COM-OKTA-AUTHENTICATOR://deviceChallenge?challengeRequest=eyJraWQ.jwt")]
    public void IsOktaVerifyDeepLink_ShouldAcceptTheOktaVerifyScheme(string uri)
    {
        OktaVerifyAppLauncher.IsOktaVerifyDeepLink(uri).ShouldBeTrue();
    }
}
