// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

public class TypeRegistrar(IServiceCollection services) : ITypeRegistrar
{
    public ITypeResolver Build() => new TypeResolver(services.BuildServiceProvider());

    public void Register(Type service, Type implementation) => services.AddSingleton(service, new TypeWithPublicConstructors(implementation).Type);

    public void RegisterInstance(Type service, object implementation) => services.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        services.AddSingleton(service, _ => factory());
    }

    private sealed class TypeWithPublicConstructors(Type type)
    {
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]
        public Type Type { get; } = type;
    }

    private sealed class TypeResolver(IServiceProvider provider) : ITypeResolver, IDisposable
    {
        public object? Resolve(Type? type) => type is not null ? provider.GetService(type) : null;

        public void Dispose()
        {
            if (provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
