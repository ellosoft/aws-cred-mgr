// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

public class ConfigInterceptor(IConfigManager configManager) : ICommandInterceptor
{
    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (configManager.ToolConfig.AwsIgnoreConfiguredEndpoints)
        {
            Environment.SetEnvironmentVariable("AWS_IGNORE_CONFIGURED_ENDPOINTS", "true");
        }
    }
}
