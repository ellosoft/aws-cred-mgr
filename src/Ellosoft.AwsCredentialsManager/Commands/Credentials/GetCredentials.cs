// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("get-credentials"), Alias("get")]
[Description("Get AWS credentials for an existing credential profile")]
[Examples(
    "get prod",
    "get prod --aws-profile default")]
public class GetCredentials(
    CredentialsManager credentialsManager,
    AwsOktaSessionManager sessionManager
) : AsyncCommand<GetCredentials.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "[CREDENTIAL_NAME]")]
        [Description("AWS credential profile name (use [italic blue]cred new[/] to create a new profile)")]
        public string? Credential { get; set; }

        [CommandOption("-p|--aws-profile")]
        [Description("AWS profile to use (profile used in AWS CLI)")]
        [DefaultValue("default")]
        public string? AwsProfile { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var credential = settings.Credential ?? credentialsManager.GetCredentialNameFromUser();

        var awsCredentials = await sessionManager.CreateOrResumeSessionAsync(credential, settings.AwsProfile);

        if (awsCredentials is null)
            return 1;

        AnsiConsole.MarkupLine($"[green]AWS credentials stored in the '{settings.AwsProfile}' AWS profile[/]");

        return 0;
    }
}
