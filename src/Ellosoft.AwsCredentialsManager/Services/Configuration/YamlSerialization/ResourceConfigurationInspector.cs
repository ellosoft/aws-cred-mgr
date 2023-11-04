// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using YamlDotNet.Serialization.TypeInspectors;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.YamlSerialization;

/// <summary>
///     This class is used by the YAML serializer to read variable metadata from the AppConfig
///     and used the variable placeholder (variable name) in the output file
/// </summary>
public class ResourceConfigurationInspector : TypeInspectorSkeleton
{
    private readonly ITypeInspector _innerTypeDescriptor;

    public ResourceConfigurationInspector(ITypeInspector innerTypeDescriptor) => _innerTypeDescriptor = innerTypeDescriptor;

    /// <summary>
    ///     Gets the property descriptor of ResourceConfiguration replacing the actual property value with the variable placeholder (variable name)
    ///     if the object contains variables
    /// </summary>
    /// <param name="type">object type</param>
    /// <param name="container">object container/parent</param>
    /// <returns></returns>
    public override IEnumerable<IPropertyDescriptor> GetProperties(Type type, object? container)
    {
        var resourceConfiguration = container as ResourceConfiguration;
        var hasVariables = resourceConfiguration?.HasVariables == true;

        return _innerTypeDescriptor.GetProperties(type, container)
            .Select(prop =>
            {
                if (!hasVariables || !resourceConfiguration!.Metadata.TryGetValue(GetPropertyName(prop.Name), out var configMetadata))
                    return prop;

                return new VariablePropertyDescription(prop, configMetadata.VariableContent);
            });
    }

    private static string GetPropertyName(string name) => PascalCaseNamingConvention.Instance.Apply(name);

    private sealed class VariablePropertyDescription : IPropertyDescriptor
    {
        private readonly IPropertyDescriptor _baseDescriptor;
        private readonly string? _variableValue;

        public VariablePropertyDescription(IPropertyDescriptor baseDescriptor, string? variableValue)
        {
            _baseDescriptor = baseDescriptor;
            _variableValue = variableValue;

            Order = baseDescriptor.Order;
            ScalarStyle = baseDescriptor.Type == Type ? ScalarStyle.DoubleQuoted : ScalarStyle.Plain;
        }

        public string Name => _baseDescriptor.Name;

        public bool CanWrite => false;

        public Type Type => typeof(string);

        public Type? TypeOverride { get; set; } = typeof(string);

        public ScalarStyle ScalarStyle { get; set; }

        public int Order { get; set; }

        public IObjectDescriptor Read(object target) => new ObjectDescriptor(_variableValue, Type, Type, ScalarStyle);

        public void Write(object target, object? value) => throw new NotSupportedException();

        public T? GetCustomAttribute<T>() where T : Attribute => _baseDescriptor.GetCustomAttribute<T>();
    }
}
