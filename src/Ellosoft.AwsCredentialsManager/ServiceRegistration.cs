// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.IO;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;
using Ellosoft.AwsCredentialsManager.Services.Platforms.Windows.Security;
using Ellosoft.AwsCredentialsManager.Services.Security;
using Ellosoft.AwsCredentialsManager.Services.Utilities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Ellosoft.AwsCredentialsManager;

public static class ServiceRegistration
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        // core services
        services
            .AddSingleton<IFileManager, FileManager>()
            .AddSingleton<IClipboardManager, ClipboardManager>()
            .AddSingleton<IConfigManager, ConfigManager>()
            .AddSingleton<ICredentialsManager, CredentialsManager>()
            .AddSingleton<IEnvironmentManager, EnvironmentManager>()
            .AddSingleton<IUserCredentialsManager, UserCredentialsManager>();

        // okta related services
        services
            .AddSingleton<IOktaClassicAuthenticator, OktaClassicAuthenticator>()
            .AddSingleton<IOktaLoginService, OktaLoginService>()
            .AddSingleton<IOktaMfaFactorSelector, OktaMfaFactorSelector>()
            .AddSingleton<IAwsOktaSessionManager, AwsOktaSessionManager>()
            .AddSingleton<IOktaSamlService, OktaSamlService>();

        // aws related services
        services
            .AddSingleton<IRdsTokenGenerator, RdsTokenGenerator>()
            .AddSingleton<IAwsCredentialsService, AwsCredentialsService>()
            .AddSingleton<IAwsSamlService, AwsSamlService>();

        services.AddHttpClient(nameof(OktaHttpClient), OktaHttpClient.Configure);
        services.AddKeyedSingleton(nameof(OktaHttpClient), (provider, _)
            => provider.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(OktaHttpClient)));

        services
            .AddSingleton<ICommandInterceptor, LogInterceptor>()
            .AddSingleton<ICommandInterceptor, ConfigInterceptor>();

        if (OperatingSystem.IsMacOS())
        {
            RegisterMacOSServices(services);
        }

        if (OperatingSystem.IsWindows())
        {
            RegisterWindowsServices(services);
        }

        // fallback implementations
        services.TryAddSingleton<ISecureStorage, SecureStorage>();

        return services;
    }

    [SupportedOSPlatform("windows")]
    private static void RegisterWindowsServices(IServiceCollection services)
    {
        services.AddSingleton<ISecureStorage, SecureStorageWindows>();

        // platform services
        services.AddSingleton<IProtectedDataService, ProtectedDataService>();
    }

    [SupportedOSPlatform("macos")]
    private static void RegisterMacOSServices(IServiceCollection services)
    {
        services.AddSingleton<ISecureStorage, SecureStorageMacOS>();

        // platform services
        services.AddSingleton<IKeychainService, KeychainService>();
        services.AddSingleton<IMacOsKeychainInterop, MacOsKeychainInterop>();
    }
}
