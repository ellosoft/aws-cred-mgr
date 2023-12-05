// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Collections;
using System.Reflection;
using System.Text;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration;

public class ConfigReader
{
    private static readonly IDeserializer Deserializer = CreateDeserializer();

    public AppConfig Read(string filePath)
    {
        var hasVariables = false;
        VariablesSection? variableConfig = null;
        Dictionary<string, object>? formattedVariables = null;

        var yamlContent = new StringBuilder();
        var formattedYamlContent = new StringBuilder();

        using var fileReader = new StreamReader(filePath);

        while (fileReader.ReadLine() is { } line)
        {
            if (line.Contains("---") && line.Trim() == "---")
            {
                hasVariables = TryGetVariables(yamlContent, out variableConfig, out formattedVariables);
                yamlContent.Clear();

                continue;
            }

            if (hasVariables)
            {
                var formattedLine = ReplaceVariables(formattedVariables!, line);
                formattedYamlContent.AppendLine(formattedLine);
            }

            yamlContent.AppendLine(line);
        }

        var rawConfigContent = yamlContent.ToString();

        static AppConfig DeserializeAppConfig(string content)
            => Deserializer.Deserialize<AppConfig?>(content) ?? new AppConfig();

        if (!hasVariables)
            return DeserializeAppConfig(rawConfigContent);

        var appConfig = DeserializeAppConfig(formattedYamlContent.ToString());
        appConfig.Variables = variableConfig;

        var originalConfig = Deserializer.Deserialize<object?>(rawConfigContent);

        if (originalConfig is not null)
            UpdateConfigMetadata(appConfig, originalConfig);

        return appConfig;
    }

    private static void UpdateConfigMetadata(object? obj, object yamlObject)
    {
        if (obj is null)
            return;

        var type = obj.GetType();

        if (type.IsPrimitive || type == typeof(string) || type == typeof(DateTime))
            return;

        var complexYamlObj = (IDictionary<object, object>)yamlObject;

        if (obj is IDictionary dictionary)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                if (!complexYamlObj.TryGetValue(GetYamlName(entry.Key.ToString()!), out var yamlValue))
                    continue;

                if (yamlValue is string stringValue && stringValue.Contains("${"))
                    throw new NotSupportedException($"Variables are not support for the key {entry.Key}");

                UpdateConfigMetadata(entry.Value!, (IDictionary<object, object>)yamlValue);
            }

            return;
        }

        foreach (var property in type.GetProperties())
        {
            CheckPropertyForVariable(property, obj, complexYamlObj);
        }
    }

    private static void CheckPropertyForVariable(PropertyInfo property, object containerObj, IDictionary<object, object> yamlObject)
    {
        if (!yamlObject.TryGetValue(GetYamlName(property.Name), out var yamlValue))
            return;

        if (yamlValue is string stringValue && stringValue.Contains("${") && containerObj is ResourceConfiguration resourceConfig)
        {
            resourceConfig.Metadata[property.Name] = new ConfigMetadata
            {
                VariableContent = yamlValue.ToString()
            };

            return;
        }

        var propValue = property.GetValue(containerObj);
        UpdateConfigMetadata(propValue, yamlValue);
    }

    private static bool TryGetVariables(StringBuilder yamlContent, out VariablesSection? variableConfig, out Dictionary<string, object>? formattedVariables)
    {
        var variablesDefinition = yamlContent.ToString();
        variableConfig = Deserializer.Deserialize<VariablesSection>(variablesDefinition);

        if (variableConfig.Variables?.Count > 0)
        {
            ValidateVariable(variableConfig.Variables);
            formattedVariables = FormatKeyAsPlaceholders(variableConfig.Variables);

            return true;
        }

        formattedVariables = null;

        return false;
    }

    private static void ValidateVariable(Dictionary<string, object> variables)
    {
        if (variables.Values.Any(v => v is IDictionary))
            throw new NotSupportedException("Complex variable types are not supported yet");
    }

    private static string ReplaceVariables(Dictionary<string, object> variables, string configLine)
    {
        const string VARIABLE_PREFIX = "${";

        if (!configLine.Contains(VARIABLE_PREFIX))
            return configLine;

        foreach (var (placeholder, value) in variables)
        {
            if (configLine.Contains(placeholder))
                configLine = configLine.Replace(placeholder, value.ToString());
        }

        return configLine;
    }

    private static Dictionary<string, object> FormatKeyAsPlaceholders(Dictionary<string, object> value)
        => value.ToDictionary(kv => $"${{{kv.Key}}}", kv => kv.Value);

    private static IDeserializer CreateDeserializer() =>
        new DeserializerBuilder()
            .IgnoreUnmatchedProperties()
            .WithNamingConvention(UnderscoredNamingConvention.Instance)
            .Build();

    private static string GetYamlName(string value) => UnderscoredNamingConvention.Instance.Apply(value);
}
