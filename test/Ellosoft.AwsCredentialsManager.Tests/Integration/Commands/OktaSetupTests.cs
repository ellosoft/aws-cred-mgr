// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Commands;

public class OktaSetupTests(ITestOutputHelper outputHelper, TestFixture testFixture)
    : IntegrationTest(outputHelper, testFixture)
{
    [Fact]
    public void OktaSetup_Interactive_ShouldCreateNewProfile()
    {
        var app = new TestCommandApp(Services);
        app.Configure(config =>
        {
            config.AddBranch<OktaBranch>(okta =>
            {
                okta.AddCommand<SetupOkta>();
            });
        });

        app.Console.Input.PushTextWithEnter("https://xyz.okta.com");
        app.Console.Input.PushTextWithEnter("john");
        app.Console.Input.PushTextWithEnter("john's password");

        var (exitCode, output) = app.Run("okta", "setup");

        exitCode.Should().Be(0);
        output.Should().Contain("All good");
    }
}
