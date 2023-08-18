// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Reflection;
using Ellosoft.AwsCredentialsManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Logging;

internal static class ServiceRegistration
{
    private const long MAX_LOG_FILE_SIZE = (20 * 1024) ^ 2;

    private static readonly string LogFileName = AppDataDirectory.GetPath($"{Assembly.GetExecutingAssembly().GetName().Name}.log");

    public static IServiceCollection AddAppLogging(this IServiceCollection services)
    {
        return services.AddLogging(config =>
            config.AddSerilog(new LoggerConfiguration()
                .MinimumLevel.ControlledBy(LogInterceptor.LogLevel)
                .WriteTo.File(LogFileName, fileSizeLimitBytes: MAX_LOG_FILE_SIZE)
                .CreateLogger()
            )
        );
    }
}
