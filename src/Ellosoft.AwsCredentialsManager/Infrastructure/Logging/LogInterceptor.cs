// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Serilog.Core;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

public class LogInterceptor : ICommandInterceptor
{
    public static readonly LoggingLevelSwitch LogLevel = new();

    public void Intercept(CommandContext context, CommandSettings settings)
    {
        if (settings is ILogLevelSettings logSettings)
        {
            LogLevel.MinimumLevel = logSettings.LogLevel;
        }
    }
}
