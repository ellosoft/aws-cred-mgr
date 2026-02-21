// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services;
using Ellosoft.AwsCredentialsManager.Services.IO;

namespace Ellosoft.AwsCredentialsManager.Commands.Config;

[Name("aws")]
[Description("Open AWS credentials file")]
[Examples("aws")]
public class OpenAwsConfig(IFileManager fileManager) : Command
{
    public override int Execute(CommandContext context, CancellationToken cancellationToken)
    {
        var awsCredentialsPath = Path.Combine(AppDataDirectory.UserProfileDirectory, ".aws", "credentials");

        if (!File.Exists(awsCredentialsPath))
            throw new CommandException($"The file {awsCredentialsPath} does not exist");

        fileManager.OpenFileUsingDefaultApp(awsCredentialsPath);

        return 0;
    }
}
