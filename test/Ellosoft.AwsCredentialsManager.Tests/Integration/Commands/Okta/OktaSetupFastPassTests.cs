// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text.Json;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Idx;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Security;
using Ellosoft.AwsCredentialsManager.Tests.Integration.FakeApis;
using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Commands.Okta;

public sealed class OktaSetupFastPassTests : IntegrationTest
{
    private readonly string _profileName = Guid.NewGuid().ToString("N");
    private readonly IConfigManager _configManager = Substitute.For<IConfigManager>();

    public OktaSetupFastPassTests(ITestOutputHelper outputHelper, TestFixture testFixture) : base(outputHelper, testFixture)
    {
        _configManager.AppConfig.Returns(new AppConfig());
        _configManager.ToolConfig.Returns(new ActiveToolConfiguration(new ToolConfiguration()));

        var userCredentialsManager = Substitute.For<IUserCredentialsManager>();
        userCredentialsManager.SupportCredentialsStore.Returns(false);

        // keep the test hermetic (no real config file / keychain) and route the IDX + loopback traffic to the fake Okta API
        AppServices.Replace(ServiceDescriptor.Singleton(_configManager));
        AppServices.Replace(ServiceDescriptor.Singleton(userCredentialsManager));
        AppServices.Replace(ServiceDescriptor.Singleton<IOktaIdxHttpClientFactory>(
            new TestOktaIdxHttpClientFactory(TestFixture.WebApp.GetTestServer(), TestCorrelationId)));
    }

    [Fact]
    public void OktaSetup_WithFastPass_ShouldOpenOktaVerifyAppAndCreateProfile()
    {
        // the user approves the sign-in in the Okta Verify app opened through the deep link
        var appLauncher = Substitute.For<IOktaVerifyAppLauncher>();
        appLauncher.TryLaunch(Arg.Any<string>()).Returns(_ =>
        {
            OktaIdxController.ApproveInApp(TestCorrelationId);
            return true;
        });

        AppServices.Replace(ServiceDescriptor.Singleton(appLauncher));

        var (domain, username, password) = RunOktaSetupWithFastPass();

        var requests = TestRequestsFilter.Requests[TestCorrelationId];
        var paths = requests.Select(r => r.Request.RequestUri!.AbsolutePath).ToList();

        paths.Take(3).ShouldBe(["/oauth2/v1/authorize", "/idp/idx/introspect", "/idp/idx/identify"]);
        paths.ShouldContain("/idp/idx/authenticators/poll/cancel");
        paths.ShouldContain("/idp/idx/authenticators/okta-verify/launch");
        paths.ShouldContain("/idp/idx/authenticators/poll");
        paths.ShouldNotContain("/probe");
        paths.ShouldNotContain("/challenge");
        paths.ShouldNotContain("/api/v1/authn");
        paths.TakeLast(2).ShouldBe(["/login/token/redirect", "/api/v1/sessions/me"]);

        var signInPageUri = requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/oauth2/v1/authorize").Request.RequestUri!;
        signInPageUri.Host.ShouldBe(new Uri(domain).Host);
        signInPageUri.Query.ShouldContain($"client_id={OktaIdxAuthenticator.OktaDashboardClientId}");
        signInPageUri.Query.ShouldContain($"redirect_uri={Uri.EscapeDataString($"{domain}/enduser/callback")}");

        var introspect = (JsonElement)requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/idp/idx/introspect").RequestModel!;
        introspect.GetProperty("stateToken").GetString().ShouldBe(OktaIdxController.StateToken);

        var identify = (JsonElement)requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/idp/idx/identify").RequestModel!;
        identify.GetProperty("identifier").GetString().ShouldBe(username);
        identify.GetProperty("credentials").GetProperty("passcode").GetString().ShouldBe(password);
        identify.GetProperty("stateHandle").GetString().ShouldBe(OktaIdxController.StateHandle);

        var cancel = (JsonElement)requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/idp/idx/authenticators/poll/cancel").RequestModel!;
        cancel.GetProperty("reason").GetString().ShouldBe("OV_UNREACHABLE_BY_LOOPBACK");

        appLauncher.Received(1).TryLaunch($"com-okta-authenticator:/deviceChallenge?challengeRequest={OktaIdxController.ChallengeRequest}");

        AssertProfileCreated(domain);
    }

    [Fact]
    public void OktaSetup_WithFastPass_WhenOktaDoesNotOfferToOpenTheApp_ShouldAuthenticateThroughOktaVerifyLoopback()
    {
        OktaIdxController.SetOffersAppLaunch(TestCorrelationId, false);

        var (domain, _, _) = RunOktaSetupWithFastPass();

        var requests = TestRequestsFilter.Requests[TestCorrelationId];
        var paths = requests.Select(r => r.Request.RequestUri!.AbsolutePath).ToList();

        paths.ShouldContain("/idp/idx/authenticators/poll/cancel");
        paths.ShouldContain("/probe");
        paths.ShouldContain("/challenge");
        paths.ShouldContain("/idp/idx/authenticators/poll");
        paths.ShouldNotContain("/idp/idx/authenticators/okta-verify/launch");
        paths.TakeLast(2).ShouldBe(["/login/token/redirect", "/api/v1/sessions/me"]);

        var challenge = requests.Single(r => r.Request.RequestUri!.AbsolutePath == "/challenge");
        ((JsonElement)challenge.RequestModel!).GetProperty("challengeRequest").GetString().ShouldBe(OktaIdxController.ChallengeRequest);
        challenge.Request.RequestUri!.Port.ShouldBe(8769);
        challenge.Request.Headers.GetValues("Origin").ShouldBe([domain]);

        AssertProfileCreated(domain);
    }

    private (string Domain, string Username, string Password) RunOktaSetupWithFastPass()
    {
        App.Configure(config =>
            config.AddBranch<OktaBranch>(okta =>
                okta.AddCommand<SetupOkta>()));

        var domain = $"https://{Faker.Internet.DomainWord()}.okta.com";
        var username = Faker.Internet.UserName();
        var password = Faker.Internet.Password();

        App.Console.Input.PushTextWithEnter(password);

        var (exitCode, output) = App.Run("okta", "setup", _profileName, "-d", domain, "-u", username, "--mfa", "fastpass");

        exitCode.ShouldBe(0);
        output.ShouldContain("Okta FastPass");
        output.ShouldContain("Authenticated!");
        output.ShouldContain("All good");

        return (domain, username, password);
    }

    private void AssertProfileCreated(string domain)
    {
        var oktaProfile = _configManager.AppConfig.Authentication.ShouldNotBeNull().Okta[_profileName];
        oktaProfile.OktaDomain.ShouldBe($"{domain}/");
        oktaProfile.PreferredMfaType.ShouldBe(OktaMfaFactorSelector.FastPassFactorCode);
        _configManager.Received(1).SaveConfig();
    }

    /// <summary>
    ///     Sends both the Okta (IDX) and the Okta Verify loopback traffic to the in-memory test server
    /// </summary>
    private sealed class TestOktaIdxHttpClientFactory(TestServer server, string correlationId) : IOktaIdxHttpClientFactory
    {
        public HttpClient CreateSessionClient(CookieContainer cookieContainer) => Create();

        public HttpClient CreateLoopbackClient() => Create();

        private HttpClient Create() => new(new CorrelationIdMessageHandler(correlationId) { InnerHandler = server.CreateHandler() });
    }
}
