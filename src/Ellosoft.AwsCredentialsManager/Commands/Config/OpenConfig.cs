// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.IO;

namespace Ellosoft.AwsCredentialsManager.Commands.Config;

[Name("user")]
[Description("Open credentials manager user config file (default)")]
[Examples("user")]
public class OpenConfig(IConfigManager configManager, IFileManager fileManager) : Command
{
    public override int Execute(CommandContext context)
    {
        if (!File.Exists(configManager.AppConfigPath))
            configManager.SaveConfig();

        fileManager.OpenFile(configManager.AppConfigPath);

        return 0;
    }
}
