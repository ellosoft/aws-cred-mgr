// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

#pragma warning disable IDE0005

using Ellosoft.AwsCredentialsManager.Commands.Config;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Microsoft.Extensions.DependencyInjection;
using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;
using Ellosoft.AwsCredentialsManager.Services.Security;
using FluentAssertions;
using Spectre.Console.Cli;
using Spectre.Console.Testing;

namespace Ellosoft.AwsCredentialsManager.Tests;

public class ServiceRegistrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    public ServiceRegistrationTests()
    {
        _services = new ServiceCollection();

        _services.AddLogging();
        _services.RegisterAppServices();

        _serviceProvider = _services.BuildServiceProvider();
    }

    [Theory]
    // okta
    [InlineData(typeof(SetupOkta))]
    // config
    [InlineData(typeof(OpenAwsConfig))]
    [InlineData(typeof(OpenConfig))]
    // credentials
    [InlineData(typeof(CreateCredentialsProfile))]
    [InlineData(typeof(GetCredentials))]
    [InlineData(typeof(ListCredentialsProfiles))]
    // rds
    [InlineData(typeof(GetRdsPassword))]
    [InlineData(typeof(ListRdsProfiles))]
    public void GetService_UsingCommands_ShouldResolveDependentServices(Type commandType)
    {
        var registrar = new TypeRegistrar(_services);

        var app = new CommandAppTester(registrar);
        app.Configure(config => config.PropagateExceptions());

        var setDefaultCommandMethod = app.GetType().GetMethod(nameof(CommandAppTester.SetDefaultCommand));
        var genericMethod = setDefaultCommandMethod!.MakeGenericMethod(commandType);
        genericMethod.Invoke(app, [null, null]);

        try
        {
            _ = app.Run("__default_command");
        }
        catch (Exception ex)
        {
            ex.InnerException?.Message.Should().NotContain("Unable to resolve service for type");
        }
    }

#if MACOS
    [Theory]
    [InlineData(typeof(ISecureStorage), typeof(SecureStorageMacOS))]
    [InlineData(typeof(IKeychainService), typeof(KeychainService))]
    [InlineData(typeof(IMacOsKeychainInterop), typeof(MacOsKeychainInterop))]
    public void MacOS_GetService_ShouldResolveService(Type serviceType, Type implementationType)
    {
        TestServiceResolution(serviceType, implementationType);
    }
#endif

#if WINDOWS
    [Theory]
    [InlineData(typeof(ISecureStorage), typeof(SecureStorageWindows))]
    [InlineData(typeof(IProtectedDataService), typeof(ProtectedDataService))]
    public void Windows_GetService_ShouldResolveService(Type serviceType, Type implementationType)
    {
        TestServiceResolution(serviceType, implementationType);
    }
#endif

    private void TestServiceResolution(Type serviceType, Type implementationType)
    {
        var resolvedServices = _serviceProvider.GetServices(serviceType).ToList();

        resolvedServices.Should().Contain(s => s!.GetType().IsAssignableTo(implementationType));
    }
}
