// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("get-credentials"), Alias("get")]
[Description("Get AWS credentials for an existing credential profile")]
[Examples(
    "get prod",
    "get prod --aws-profile default")]
public class GetCredentials : Command<AwsSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] AwsSettings settings)
    {
        var credentialsService = new AwsCredentialsService();

#pragma warning disable S1481 // Unused local variables should be removed
        var x = credentialsService.GetCredentialsFromStore("default");
#pragma warning restore S1481 // Unused local variables should be removed

        return 0;
    }
}
