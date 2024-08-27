// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("list"), Alias("ls")]
[Description("List saved AWS RDS profiles")]
[Examples("ls")]
public class ListRdsProfiles : Command<ListRdsProfiles.Settings>
{
    public class Settings : CommonSettings
    {
        [CommandOption("--env")]
        [Description("Environment name")]
        public string? Environment { get; set; }
    }

    private readonly IConfigManager _configManager;

    public ListRdsProfiles(IConfigManager configManager) => _configManager = configManager;

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var environments = settings.Environment is null
            ? _configManager.AppConfig.Environments
            : _configManager.AppConfig.Environments.Where(k => k.Key == settings.Environment);

        foreach (var (envName, env) in environments)
            RenderTable(envName, env);

        return 0;
    }

    private void RenderTable(string environmentName, EnvironmentConfiguration env)
    {
        var table = new Table()
            .Title($"[green]Environment: {environmentName}[/]")
            .AddColumn("Name")
            .AddColumn("Hostname")
            .AddColumn("Port")
            .AddColumn("Username")
            .AddColumn("Region")
            .AddColumn("Credential")
            .AddColumn("Template")
            .Expand()
            .Caption("[grey62][[*]] Inherited value[/]");

        foreach (var (rdsName, dbConfig) in env.Rds.OrderBy(kv => kv.Key))
        {
            var template = GetRdsTemplate(dbConfig.Template, _configManager.AppConfig);

            table.AddRow(
                rdsName,
                PrintValue(dbConfig.Hostname, template?.Hostname),
                PrintValue(dbConfig.Port?.ToString(), template?.Port?.ToString()),
                PrintValue(dbConfig.Username, template?.Username),
                PrintValue(dbConfig.Region, template?.Region),
                PrintValue(dbConfig.Credential, env.Credential),
                PrintValue(dbConfig.Template, null)
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
    }

    public static DatabaseConfiguration? GetRdsTemplate(string? templateName, AppConfig appConfig)
    {
        if (templateName is null)
            return null;

        return appConfig.Templates?.Rds.TryGetValue(templateName, out var template) == true ? template : null;
    }

    private static string PrintValue(string? value, string? inheritedValue) =>
        value ?? (inheritedValue is not null ? $"[grey62 i]{inheritedValue} *[/]" : "--");
}
