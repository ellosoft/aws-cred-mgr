// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

#pragma warning disable IDE0005

using Microsoft.Extensions.DependencyInjection;
using Ellosoft.AwsCredentialsManager.Services.Platforms.MacOS.Security;
using Ellosoft.AwsCredentialsManager.Services.Security;
using NSubstitute;
using Spectre.Console;
using Spectre.Console.Cli;

namespace Ellosoft.AwsCredentialsManager.Tests;

public class ServiceRegistrationTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IServiceCollection _services;

    public ServiceRegistrationTests()
    {
        _services = new ServiceCollection();

        _services.AddSingleton<IAnsiConsole>(_ => Substitute.For<IAnsiConsole>());
        _services.AddLogging();
        _services.AddAppServices();

        _serviceProvider = _services.BuildServiceProvider();
    }

    [Fact]
    public void GetService_UsingCommands_ShouldResolveDependentServices()
    {
        var commandTypes = typeof(ServiceRegistration).Assembly.GetTypes().Where(type =>
            type is { IsAbstract: false, IsInterface: false } &&
            typeof(ICommand).IsAssignableFrom(type)).ToList();

        var serviceProvider = _services.BuildServiceProvider();

        var commands = commandTypes
            .Select(t => ActivatorUtilities.CreateInstance(serviceProvider, t))
            .ToList();

        commands.ShouldNotBeEmpty();
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

    internal void TestServiceResolution(Type serviceType, Type implementationType)
    {
        var resolvedServices = _serviceProvider.GetServices(serviceType).ToList();

        resolvedServices.ShouldContain(s => s!.GetType().IsAssignableTo(implementationType));
    }

    public static class CommandExecutePatch
    {
        // ReSharper disable once InconsistentNaming
        // ReSharper disable once UnusedMember.Local
        public static bool Prefix(ref object __result)
        {
            __result = Task.FromResult(0);

            return false;
        }
    }
}
