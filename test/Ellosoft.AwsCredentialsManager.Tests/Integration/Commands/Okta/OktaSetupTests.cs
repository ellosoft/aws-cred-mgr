// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using Ellosoft.AwsCredentialsManager.Services.Security;
using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;
using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Commands.Okta;

public sealed class OktaSetupTests(ITestOutputHelper outputHelper, TestFixture testFixture)
    : IntegrationTest(outputHelper, testFixture), IDisposable
{
    private readonly string _profileName = Guid.NewGuid().ToString("N");

    [Fact]
    public void OktaSetup_Interactive_ShouldCreateNewProfile()
    {
        App.Configure(config =>
            config.AddBranch<OktaBranch>(okta =>
                okta.AddCommand<SetupOkta>()));

        var domain = $"https://{Faker.Internet.DomainWord()}.okta.com";
        var username = Faker.Internet.UserName();
        var password = Faker.Internet.Password();

        App.Console.Input.PushTextWithEnter(domain);
        App.Console.Input.PushTextWithEnter(username);
        App.Console.Input.PushTextWithEnter(password);
        App.Console.Input.PushTextWithEnter("Y");

        var (exitCode, output) = App.Run("okta", "setup", _profileName);

        exitCode.ShouldBe(0);
        output.ShouldContain("All good");

        TestRequestsFilter.Requests.ShouldContainKey(TestCorrelationId);
        TestRequestsFilter.Requests[TestCorrelationId][0]
            .RequestModel.ShouldBeEquivalentTo(new AuthenticationRequest
            {
                Username = username, Password = password
            });

        TestRequestsFilter.Requests[TestCorrelationId][0]
            .Request.RequestUri.ShouldBe(new Uri($"{domain}/api/v1/authn"));

        var userCredentialsService = TestFixture.WebApp.Services.GetRequiredService<IUserCredentialsManager>();

        var userCredentials = userCredentialsService.GetUserCredentials(_profileName);

        userCredentials.ShouldNotBeNull();
        userCredentials!.Username.ShouldBe(username);
        userCredentials.Password.ShouldBe(password);
    }

    public void Dispose()
    {
        var secureStorage = TestFixture.WebApp.Services.GetRequiredService<ISecureStorage>();
        secureStorage.DeleteSecret(_profileName);
    }
}
