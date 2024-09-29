// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.Utils;

public class TestTypeRegistrar(IServiceProvider appServicesProvider) : ITypeRegistrar
{
    public void Register(Type service, Type implementation) { }

    public void RegisterInstance(Type service, object implementation) { }

    public void RegisterLazy(Type service, Func<object> factory) { }

    public ITypeResolver Build() => new TypeResolve(appServicesProvider);

    private class TypeResolve(IServiceProvider appServicesProvider) : ITypeResolver
    {
        public object? Resolve(Type? type) => type is not null ? ActivatorUtilities.GetServiceOrCreateInstance(appServicesProvider, type) : null;
    }
}

public static class ServiceProviderExtensions
{
    private static Type? constantCallSiteType;
    private static Type? serviceIdentifierType;
    private static ConstructorInfo? constantCallSiteConstructor;
    private static MethodInfo? fromServiceTypeMethod;

    static ServiceProviderExtensions()
    {
        constantCallSiteType = typeof(ServiceProvider).Assembly.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.ConstantCallSite");
        serviceIdentifierType = typeof(ServiceProvider).Assembly.GetType("Microsoft.Extensions.DependencyInjection.ServiceLookup.ServiceIdentifier");

        constantCallSiteConstructor = constantCallSiteType?.GetConstructor([typeof(Type), typeof(object)]);
        fromServiceTypeMethod = serviceIdentifierType?.GetMethod("FromServiceType");
    }

    public static void AddSingletonPostBuild(this ServiceProvider serviceProvider, Type serviceType, object implementation)
    {
        var callSiteFactory = serviceProvider.GetPrivatePropertyValue<object>("CallSiteFactory");
        var serviceIdentifier = fromServiceTypeMethod.Invoke(null, [(object)typeof(TService)]);
        var objImplementation = implementationFactory(serviceProvider);
        var callSite = constantCallSiteConstructor.Invoke([typeof(TService), objImplementation]);

        callSiteFactory.CallMethod("Add", serviceIdentifier, callSite);
    }
}
