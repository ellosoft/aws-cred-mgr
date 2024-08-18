// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

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
    public static IServiceCollection RegisterAppServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IFileManager, FileManager>()
            .AddSingleton<IConfigManager, ConfigManager>()
            .AddSingleton<ICredentialsManager, CredentialsManager>()
            .AddSingleton<IEnvironmentManager, EnvironmentManager>()
            .AddSingleton<IUserCredentialsManager, UserCredentialsManager>()
            .AddSingleton<IClipboardManager, ClipboardManager>();

        // okta related services
        services
            .AddSingleton<OktaClassicAuthenticator>()
            .AddSingleton<OktaClassicAccessTokenProvider>()
            .AddSingleton<IOktaLoginService, OktaLoginService>()
            .AddSingleton<IOktaMfaFactorSelector, OktaMfaFactorSelector>()
            .AddSingleton<IAwsOktaSessionManager, AwsOktaSessionManager>()
            .AddSingleton<OktaSamlService>();

        // aws related services
        services
            .AddSingleton<RdsTokenGenerator>()
            .AddSingleton<AwsSamlService>();

        services
            .AddKeyedSingleton(nameof(OktaHttpClientFactory), OktaHttpClientFactory.CreateHttpClient());

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
