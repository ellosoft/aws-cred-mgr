// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Commands;

public class OktaSetupTests(ITestOutputHelper outputHelper, TestFixture testFixture) : IntegrationTest(outputHelper, testFixture)
{
    [Fact]
    public void OktaSetup_Interactive_ShouldCreateNewProfile()
    {
        App.Configure(config =>
        {
            config.AddBranch<OktaBranch>(okta =>
            {
                okta.AddCommand<SetupOkta>();
            });
        });

        App.Console.Input.PushTextWithEnter("https://xyz.okta.com");
        App.Console.Input.PushTextWithEnter("john");
        App.Console.Input.PushTextWithEnter("john's password");
        App.Console.Input.PushTextWithEnter("Y");

        var (exitCode, output) = App.Run("okta", "setup");

        exitCode.Should().Be(0);
        output.Should().Contain("All good");

        TestRequestsFilter.Requests.Should().ContainKey(TestCorrelationId)
            .WhoseValue[0].RequestModel.Should().BeOfType<AuthenticationRequest>()
            .Which.Should().BeEquivalentTo(new AuthenticationRequest
            {
                Username = "john", Password = "john's password"
            });
    }
}
