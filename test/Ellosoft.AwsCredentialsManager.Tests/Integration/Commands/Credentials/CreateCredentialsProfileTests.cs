// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Commands.Credentials;

public class CreateCredentialsProfileTests(ITestOutputHelper outputHelper, TestFixture testFixture) : IntegrationTest(outputHelper, testFixture)
{
    [Fact(Skip = "WIP")]
    public void CreateCredentialsProfile_Interactive_ShouldCreateNewProfile()
    {
        App.Configure(config =>
            config.AddBranch<CredentialsBranch>(cred =>
                cred.AddCommand<CreateCredentialsProfile>()));

        var profileName = Guid.NewGuid().ToString("N");

        var (exitCode, output) = App.Run("new", profileName);

        exitCode.Should().Be(0);
        output.Should().Contain($"'{profileName}' credentials created");

        var credentialsManager = TestFixture.WebApp.Services.GetRequiredService<ICredentialsManager>();
        credentialsManager.TryGetCredential(profileName, out var credentialsConfig);

        credentialsConfig.Should().NotBeNull();
        // credentialsConfig.RoleArn.Should().Be(awsRoleArn);
        // credentialsConfig.OktaProfile.Should().Be(profileName);
        // credentialsConfig.OktaAppUrl.Should().Be(oktaAppUrl);

    }
}
