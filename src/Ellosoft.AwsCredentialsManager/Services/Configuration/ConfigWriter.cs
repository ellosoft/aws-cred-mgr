// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Configuration.YamlSerialization;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration;

public class ConfigWriter
{
    private static readonly ISerializer Serializer = new SerializerBuilder()
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitNull)
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .WithTypeInspector(inner => new ResourceConfigurationInspector(inner))
        .Build();

    /// <summary>
    ///     Serializes an AppConfig into a output file, replacing variable values with variable placeholders <see cref="ResourceConfigurationInspector" />
    /// </summary>
    /// <param name="fileName">Output file name</param>
    /// <param name="config">AppConfig object</param>
    public void Write(string fileName, AppConfig config)
    {
        var writer = new StringBuilder();

        if (config.Variables is not null)
        {
            writer.AppendLine(Serializer.Serialize(config.Variables));
            writer.AppendLine("---");
            writer.AppendLine();
        }

        WriteProperty(writer, nameof(AppConfig.Authentication), config.Authentication);
        WriteProperty(writer, nameof(AppConfig.Templates), config.Templates);
        WriteProperty(writer, nameof(AppConfig.Credentials), config.Credentials);
        WriteProperty(writer, nameof(AppConfig.Environments), config.Environments);

        CreateBackupFile(fileName);
        File.WriteAllText(fileName, writer.ToString(), Encoding.UTF8);
        DeleteBackupSafe(fileName);
    }

    private static void WriteProperty(StringBuilder writer, string propertyName, object? value)
    {
        if (value is null)
            return;

        var yamlPropertyContainer = new Dictionary<object, object?>
        {
            [GetYamlPropertyName(propertyName)] = value
        };

        writer.AppendLine(Serializer.Serialize(yamlPropertyContainer));
    }

    private static string GetYamlPropertyName(string name) => UnderscoredNamingConvention.Instance.Apply(name);

    private static void CreateBackupFile(string fileName)
    {
        if (File.Exists(fileName))
            File.Copy(fileName, $"{fileName}.bak", overwrite: true);
    }

    private static void DeleteBackupSafe(string fileName)
    {
        try
        {
            File.Delete($"{fileName}.bak");
        }
        catch (Exception e)
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Unable to delete backup file: {e.Message}[/]");
        }
    }
}
