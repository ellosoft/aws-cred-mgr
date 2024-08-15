// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.IO;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;
using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager;

public static class ServiceRegistration
{
    public static IServiceCollection RegisterAppServices(this IServiceCollection services)
    {
        services
            .AddSingleton<IFileManager, FileManager>()
            .AddSingleton<IConfigManager, ConfigManager>()
            .AddSingleton<CredentialsManager>()
            .AddSingleton<EnvironmentManager>();

        // okta related services
        services
            .AddSingleton<OktaClassicAuthenticator>()
            .AddSingleton<OktaClassicAccessTokenProvider>()
            .AddSingleton<IOktaLoginService, OktaLoginService>()
            .AddSingleton<IOktaMfaFactorSelector, OktaMfaFactorSelector>()
            .AddSingleton<AwsOktaSessionManager>()
            .AddSingleton<OktaSamlService>();

        // aws related services
        services
            .AddSingleton<RdsTokenGenerator>()
            .AddSingleton<AwsSamlService>();

        services
            .AddKeyedSingleton(nameof(OktaHttpClientFactory), OktaHttpClientFactory.CreateHttpClient());

        services
            .AddSingleton<IKeychain, Keychain>();

        return services;
    }
}
