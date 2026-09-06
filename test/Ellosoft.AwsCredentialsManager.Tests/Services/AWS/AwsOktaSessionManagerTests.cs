// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Amazon.Runtime;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using NSubstitute;

namespace Ellosoft.AwsCredentialsManager.Tests.Services.AWS;

public class AwsOktaSessionManagerTests
{
    private const string CredentialProfile = "prod";
    private const string AwsProfile = "default";
    private const string RoleArn = "arn:aws:iam::123:role/TestRole";
    private const string IdpArn = "arn:aws:iam::123456:saml-provider/okta";
    private const string OktaProfile = "default";
    private const string OktaAppUrl = "https://xyz.okta.com/home/amazon_aws/abc/272";

    private readonly ICredentialsManager _credentialsManager = Substitute.For<ICredentialsManager>();
    private readonly IOktaLoginService _loginService = Substitute.For<IOktaLoginService>();
    private readonly IOktaSamlService _oktaSamlService = Substitute.For<IOktaSamlService>();
    private readonly IAwsCredentialsService _awsCredentialsService = Substitute.For<IAwsCredentialsService>();
    private readonly IAwsSamlService _awsSamlService = Substitute.For<IAwsSamlService>();
    private readonly AwsOktaSessionManager _sessionManager;

    public AwsOktaSessionManagerTests()
    {
        _sessionManager = new AwsOktaSessionManager(
            _credentialsManager,
            _loginService,
            _oktaSamlService,
            _awsCredentialsService,
            _awsSamlService);

        var credentialsConfig = new CredentialsConfiguration
        {
            RoleArn = RoleArn,
            AwsProfile = AwsProfile,
            OktaProfile = OktaProfile,
            OktaAppUrl = OktaAppUrl
        };

        _credentialsManager.TryGetCredential(CredentialProfile, out Arg.Any<CredentialsConfiguration?>())
            .Returns(x =>
            {
                x[1] = credentialsConfig;

                return true;
            });
    }

    [Fact]
    public async Task CreateOrResumeSessionAsync_WhenCachedCredentialsValid_ShouldResumeWithoutLogin()
    {
        var cached = CreateCachedCredentials(DateTime.Now.AddHours(2));
        _awsCredentialsService.GetCredentialsFromStore(AwsProfile).Returns(cached);

        var result = await _sessionManager.CreateOrResumeSessionAsync(CredentialProfile, AwsProfile);

        result.ShouldNotBeNull();
        result.ShouldBeOfType<SessionAWSCredentials>();
        await _loginService.DidNotReceive().InteractiveLogin(Arg.Any<string>(), Arg.Any<bool>());
        await _awsCredentialsService.DidNotReceive().GetAwsCredentials(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>());
    }

    [Fact]
    public async Task CreateOrResumeSessionAsync_WhenForceRenew_ShouldLoginEvenIfCacheValid()
    {
        var cached = CreateCachedCredentials(DateTime.Now.AddHours(2));
        _awsCredentialsService.GetCredentialsFromStore(AwsProfile).Returns(cached);

        var authResult = new AuthenticationResult
        {
            OktaDomain = new Uri("https://xyz.okta.com/"),
            Authenticated = true,
            SessionToken = "session-token"
        };

        _loginService.InteractiveLogin(OktaProfile).Returns(authResult);

        _oktaSamlService.GetAppSamlDataAsync(authResult, OktaAppUrl)
            .Returns(new SamlData("saml-assertion", "https://signin.aws.amazon.com", "relay"));

        _awsSamlService.GetAwsRolesAndIdpFromSamlAssertion("saml-assertion")
            .Returns(new Dictionary<string, string> { [RoleArn] = IdpArn });

        var freshCredentials = new AwsCredentialsData(
            "NEW_KEY",
            "new-secret",
            "new-token",
            DateTime.Now.AddHours(2),
            RoleArn);

        _awsCredentialsService.GetAwsCredentials("saml-assertion", RoleArn, IdpArn)
            .Returns(freshCredentials);

        var result = await _sessionManager.CreateOrResumeSessionAsync(CredentialProfile, AwsProfile, forceRenew: true);

        result.ShouldNotBeNull();
        await _loginService.Received(1).InteractiveLogin(OktaProfile);
        await _awsCredentialsService.Received(1).GetAwsCredentials("saml-assertion", RoleArn, IdpArn);
        _awsCredentialsService.Received(1).StoreCredentials(AwsProfile, freshCredentials);
    }

    [Fact]
    public async Task CreateOrResumeSessionAsync_WhenLoginReturnsSessionIdOnly_ShouldStillRetrieveSamlAssertion()
    {
        _awsCredentialsService.GetCredentialsFromStore(AwsProfile).Returns((AwsCredentialsData?)null);

        // Okta FastPass (Identity Engine) logins produce a session id, not a session token
        var authResult = new AuthenticationResult
        {
            OktaDomain = new Uri("https://xyz.okta.com/"),
            Authenticated = true,
            SessionId = "102sid",
            MfaUsed = "signed_nonce"
        };

        _loginService.InteractiveLogin(OktaProfile).Returns(authResult);

        _oktaSamlService.GetAppSamlDataAsync(authResult, OktaAppUrl)
            .Returns(new SamlData("saml-assertion", "https://signin.aws.amazon.com", "relay"));

        _awsSamlService.GetAwsRolesAndIdpFromSamlAssertion("saml-assertion")
            .Returns(new Dictionary<string, string> { [RoleArn] = IdpArn });

        _awsCredentialsService.GetAwsCredentials("saml-assertion", RoleArn, IdpArn)
            .Returns(new AwsCredentialsData("KEY", "secret", "token", DateTime.Now.AddHours(2), RoleArn));

        var result = await _sessionManager.CreateOrResumeSessionAsync(CredentialProfile, AwsProfile);

        result.ShouldNotBeNull();
        await _oktaSamlService.Received(1).GetAppSamlDataAsync(authResult, OktaAppUrl);
    }

    [Fact]
    public async Task CreateOrResumeSessionAsync_WhenLoginHasNoSession_ShouldReturnNull()
    {
        _awsCredentialsService.GetCredentialsFromStore(AwsProfile).Returns((AwsCredentialsData?)null);

        _loginService.InteractiveLogin(OktaProfile).Returns(new AuthenticationResult
        {
            OktaDomain = new Uri("https://xyz.okta.com/"),
            Authenticated = false
        });

        var result = await _sessionManager.CreateOrResumeSessionAsync(CredentialProfile, AwsProfile);

        result.ShouldBeNull();
        await _oktaSamlService.DidNotReceiveWithAnyArgs().GetAppSamlDataAsync(default!, default!);
    }

    private static AwsCredentialsData CreateCachedCredentials(DateTime expiration) =>
        new("CACHED_KEY", "cached-secret", "cached-token", expiration, RoleArn);
}
