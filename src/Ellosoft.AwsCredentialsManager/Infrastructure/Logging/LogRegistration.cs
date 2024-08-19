// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

internal static class LogRegistration
{
    private const long MAX_LOG_FILE_SIZE = 20 * (1024 ^ 2);

    private static readonly string LogFileName = AppDataDirectory.GetPath($"{AppMetadata.AppName}.log");

    public static IServiceCollection SetupLogging(this IServiceCollection services, ILogger logger)
        => services.AddLogging(config => config.AddSerilog(logger));

    public static ILogger CreateNewLogger()
        => new LoggerConfiguration()
            .MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
            .WriteTo.File(LogFileName, fileSizeLimitBytes: MAX_LOG_FILE_SIZE)
            .CreateLogger();
}
