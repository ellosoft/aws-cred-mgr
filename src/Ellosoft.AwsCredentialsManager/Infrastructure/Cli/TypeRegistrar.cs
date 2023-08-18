// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Microsoft.Extensions.DependencyInjection;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli;

public class TypeRegistrar : ITypeRegistrar
{
    private readonly IServiceCollection _builder;

    public TypeRegistrar(IServiceCollection builder) => _builder = builder;

    public ITypeResolver Build() => new TypeResolver(_builder.BuildServiceProvider());

    public void Register(Type service, Type implementation) => _builder.AddSingleton(service, implementation);

    public void RegisterInstance(Type service, object implementation) => _builder.AddSingleton(service, implementation);

    public void RegisterLazy(Type service, Func<object> factory)
    {
        ArgumentNullException.ThrowIfNull(factory);
        _builder.AddSingleton(service, _ => factory());
    }

    private sealed class TypeResolver : ITypeResolver, IDisposable
    {
        private readonly IServiceProvider _provider;

        public TypeResolver(IServiceProvider provider) => _provider = provider;

        public object? Resolve(Type? type) => type is not null ? _provider.GetService(type) : null;

        public void Dispose()
        {
            if (_provider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}
