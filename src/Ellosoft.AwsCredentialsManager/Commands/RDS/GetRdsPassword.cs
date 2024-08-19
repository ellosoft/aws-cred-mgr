// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Amazon;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Utilities;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("get-password"), Alias("pwd")]
[Description("Get AWS RDS DB password")]
[Examples(
    "pwd prod_db",
    "pwd -h localhost -p 5432 -u john")]
public class GetRdsPassword(
    IConfigManager configManager,
    IEnvironmentManager envManager,
    ICredentialsManager credentialsManager,
    IAwsOktaSessionManager awsSessionManager,
    IRdsTokenGenerator rdsTokenGenerator,
    IClipboardManager clipboardManager)
    : AsyncCommand<GetRdsPassword.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "[PROFILE]")]
        [Description("RDS profile name (use [italic blue]rds pwd[/] to create a new profile)")]
        public string? Profile { get; set; }

        [CommandOption("-h|--hostname")]
        [Description("DB instance endpoint/hostname")]
        public string? Hostname { get; set; }

        [CommandOption("-p|--port")]
        [Description("Port number used for connecting to your DB instance")]
        public int? Port { get; set; }

        [CommandOption("-u|--username")]
        [Description("Database username/account")]
        public string? Username { get; set; }

        [CommandOption("--ttl")]
        [Description("Password lifetime in minutes (max allowed: 15 minutes)")]
        [DefaultValue(DatabaseConfiguration.DefaultTtlInMinutes)]
        public int Ttl { get; set; }

        [CommandOption("--env")]
        [Description("Environment name")]
        public string? Environment { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Profile is null)
            return await HandleAdHocRequest(settings);

        var dbConfig = GetDbConfig(configManager.AppConfig, settings.Profile, settings.Environment);

        ApplyTemplateValues(dbConfig, configManager.AppConfig.Templates);

        await GenerateDbPassword(
            dbConfig.Credential,
            dbConfig.Hostname,
            dbConfig.Port,
            dbConfig.Username,
            dbConfig.Region,
            dbConfig.GetTtl()
        );

        return 0;
    }

    private async Task<int> HandleAdHocRequest(Settings settings)
    {
        var credentialName = credentialsManager.GetCredentialNameFromUser();

        AnsiConsole.MarkupLine($"Getting RDS password using [green i]{credentialName}[/] credential profile");

        var hostname = settings.Hostname ?? AnsiConsole.Ask<string>("Enter the DB hostname:");
        var port = settings.Port ?? AnsiConsole.Ask<int>("Enter the DB port:");
        var username = settings.Username ?? AnsiConsole.Ask<string>("Enter the DB username:");
        var region = settings.GetRegion();

        await GenerateDbPassword(credentialName, hostname, port, username, region.SystemName, settings.Ttl);

        CreateNewRdsProfile(credentialName, hostname, port, username, settings.Ttl, region.SystemName, settings.Environment);

        return 0;
    }

    private async Task GenerateDbPassword(string? credential, string? hostname, int? port, string? username, string? region, int ttl)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(hostname);
            ArgumentNullException.ThrowIfNull(port);
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(region);

            var awsCredentials = await awsSessionManager.CreateOrResumeSessionAsync(credential, null);

            if (awsCredentials is null)
                throw new CommandException($"Unable to resume or create AWS session for credential '{credential}'");

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var dbPassword = rdsTokenGenerator.GenerateDbPassword(awsCredentials, regionEndpoint, hostname, port.Value, username, ttl);

            AnsiConsole.MarkupLine("[green]DB Password:[/]");
            Console.WriteLine(dbPassword);
            Console.WriteLine();

            if (configManager.ToolConfig.CopyToClipboard && clipboardManager.SetClipboardText(dbPassword))
                AnsiConsole.MarkupLine("[green]DB Password copied to clipboard[/]");
        }
        catch (ArgumentNullException e)
        {
            throw new CommandException($"Error: '{e.ParamName}' value can not be empty[/]");
        }
    }

    private void CreateNewRdsProfile(string credential, string hostname, int port, string username, int ttl, string region, string? environmentName)
    {
        if (!AnsiConsole.Confirm("Do you want to save this RDS details for future use ?"))
        {
            AnsiConsole.MarkupLine("[yellow]Ok... :([/]");

            return;
        }

        var environment = envManager.GetOrCreateEnvironment(environmentName);

        var dbConfig = new DatabaseConfiguration
        {
            Hostname = hostname,
            Port = port,
            Username = username,
            Region = region
        };

        if (ttl != DatabaseConfiguration.DefaultTtlInMinutes)
            dbConfig.Ttl = ttl;

        if (credential != environment.Credential)
            dbConfig.Credential = credential;

        var profileName = AnsiConsole.Ask<string>("Enter the RDS profile name:");

        while (!environment.Rds.TryAdd(profileName, dbConfig))
        {
            profileName = AnsiConsole.Ask<string>("There is already a RDS profile with that name, please choose another one:");
        }

        configManager.SaveConfig();

        AnsiConsole.MarkupLine($"[bold green]'{profileName}' RDS profile created[/]");
    }

    private DatabaseConfiguration GetDbConfig(AppConfig appConfig, string rdsProfile, string? environmentName)
    {
        if (environmentName is not null)
        {
            var env = envManager.GetEnvironment(environmentName);

            if (env is not null && env.Rds.TryGetValue(rdsProfile, out var dbConfig))
            {
                dbConfig.Credential ??= env.Credential;

                return dbConfig;
            }

            throw new CommandException($"Unable to find RDS profile [i]'{rdsProfile}'[/] on [i]'{environmentName}'[/] environment");
        }

        var dbConfigs = new Dictionary<string, DatabaseConfiguration>();

        foreach (var (envName, env) in appConfig.Environments)
        {
            if (env.Rds.TryGetValue(rdsProfile, out var rds))
                dbConfigs[envName] = rds;
        }

        if (dbConfigs.Count == 0)
            throw new CommandException($"Unable to find RDS profile [i]'{rdsProfile}'[/]");

        var selectedConfig = dbConfigs.Count == 1
            ? dbConfigs.First()
            : AnsiConsole.Prompt(
                new SelectionPrompt<KeyValuePair<string, DatabaseConfiguration>>()
                    .Title("Select an environment:")
                    .PageSize(10)
                    .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                    .UseConverter(kv => kv.Key)
                    .AddChoices(dbConfigs));

        selectedConfig.Value.Credential ??= appConfig.Environments[selectedConfig.Key].Credential;

        return selectedConfig.Value;
    }

    private static void ApplyTemplateValues(DatabaseConfiguration dbConfig, AppConfig.TemplatesSection? templatesSection)
    {
        if (dbConfig.Template is null)
            return;

        var rdsTemplates = templatesSection?.Rds;

        if (rdsTemplates is null || !rdsTemplates.TryGetValue(dbConfig.Template, out var templateConfig))
        {
            AnsiConsole.MarkupLine($"[yellow]Warning: Unable to find RDS template [i]'{dbConfig.Template}'[/][/]");

            return;
        }

        dbConfig.Hostname ??= templateConfig.Hostname;
        dbConfig.Port ??= templateConfig.Port;
        dbConfig.Username ??= templateConfig.Username;
        dbConfig.Region ??= templateConfig.Region;
        dbConfig.Ttl ??= templateConfig.Ttl;
    }
}
