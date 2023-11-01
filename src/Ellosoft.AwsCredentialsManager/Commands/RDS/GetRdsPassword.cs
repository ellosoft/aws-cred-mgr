// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Amazon;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("get-password"), Alias("pwd")]
[Description("Get AWS RDS DB password")]
[Examples(
    "pwd prod_db",
    "pwd -h localhost -p 5432 -u john -d postgres")]
public class GetRdsPassword : AsyncCommand<GetRdsPassword.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "[PROFILE]")]
        [Description("RDS profile name (see: [italic blue]rds create[/], for instructions on how to create a new profile)")]
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
        [Description("Password lifetime in minutes (max recommended: 15 minutes)")]
        [DefaultValue(15)]
        public int Ttl { get; set; }
    }

    private readonly IConfigManager _configManager;
    private readonly EnvironmentManager _envManager;
    private readonly CredentialsManager _credentialsManager;
    private readonly AwsOktaSessionManager _awsSessionManager;
    private readonly RdsTokenGenerator _rdsTokenGenerator;

    public GetRdsPassword(
        IConfigManager configManager,
        EnvironmentManager envManager,
        CredentialsManager credentialsManager,
        AwsOktaSessionManager awsSessionManager,
        RdsTokenGenerator rdsTokenGenerator)
    {
        _configManager = configManager;
        _envManager = envManager;
        _credentialsManager = credentialsManager;
        _awsSessionManager = awsSessionManager;
        _rdsTokenGenerator = rdsTokenGenerator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Profile is null)
            return await HandleAdHocRequest(settings);

        var dbConfig = GetDbConfig(_configManager.AppConfig, settings.Profile);

        ApplyTemplateValues(dbConfig, _configManager.AppConfig.Templates);

        await GenerateDbPassword(
            dbConfig.Credential,
            dbConfig.Hostname,
            dbConfig.Port,
            dbConfig.Username,
            dbConfig.Ttl,
            dbConfig.Region
        );

        return 0;
    }

    private async Task<int> HandleAdHocRequest(Settings settings)
    {
        var credentialName = _credentialsManager.GetCredential();
        var hostname = settings.Hostname ?? AnsiConsole.Ask<string>("Enter the DB hostname:");
        var port = settings.Port ?? AnsiConsole.Ask<int>("Enter the DB port:");
        var username = settings.Username ?? AnsiConsole.Ask<string>("Enter the DB username:");
        var region = settings.GetRegion();

        await GenerateDbPassword(credentialName, hostname, port, username, settings.Ttl, region.SystemName);

        CreateNewRdsProfile(credentialName, hostname, port, username, settings.Ttl, region.SystemName);

        return 0;
    }

    private async Task GenerateDbPassword(string? credential, string? hostname, int? port, string? username, int? ttl, string? region)
    {
        try
        {
            ArgumentNullException.ThrowIfNull(credential);
            ArgumentNullException.ThrowIfNull(hostname);
            ArgumentNullException.ThrowIfNull(port);
            ArgumentNullException.ThrowIfNull(username);
            ArgumentNullException.ThrowIfNull(ttl);
            ArgumentNullException.ThrowIfNull(region);

            var awsCredentials = await _awsSessionManager.CreateOrResumeSessionAsync(credential);

            if (awsCredentials is null)
                throw new CommandException($"Unable to resume or create AWS session for credential '{credential}'");

            var regionEndpoint = RegionEndpoint.GetBySystemName(region);
            var dbPassword = _rdsTokenGenerator.GenerateDbPassword(awsCredentials, regionEndpoint, hostname, port.Value, username, ttl.Value);

            AnsiConsole.MarkupLine($"\r\n[green]DB Password:[/]\r\n{dbPassword}\r\n");
        }
        catch (ArgumentNullException e)
        {
            throw new CommandException($"Invalid {e.ParamName} value");
        }
    }

    private void CreateNewRdsProfile(string credential, string hostname, int port, string username, int ttl, string region)
    {
        if (!AnsiConsole.Confirm("Do you want to save this RDS details for future use ?"))
        {
            AnsiConsole.MarkupLine("[yellow]Ok... :([/]");

            return;
        }

        var environment = _envManager.GetEnvironment();

        var dbConfig = new DatabaseConfiguration
        {
            Hostname = hostname,
            Port = port,
            Username = username,
            Region = region,
            Ttl = ttl
        };

        if (credential != environment.Credential)
            dbConfig.Credential = credential;

        var profileName = AnsiConsole.Ask<string>("Enter the RDS profile name:");

        while (!environment.Rds.TryAdd(profileName, dbConfig))
        {
            profileName = AnsiConsole.Ask<string>("There is already a RDS profile with that name, please choose another one:");
        }

        _configManager.SaveConfig();
    }

    private static DatabaseConfiguration GetDbConfig(AppConfig appConfig, string rdsProfile)
    {
        var dbConfigs = new Dictionary<string, DatabaseConfiguration>();

        foreach (var (envName, env) in appConfig.Environments)
        {
            if (env.Rds.TryGetValue(rdsProfile, out var rds))
                dbConfigs[envName] = rds;
        }

        if (dbConfigs.Count == 0)
            throw new CommandException($"Unable to find RDS profile '{rdsProfile}'");

        if (dbConfigs.Count == 1)
            return dbConfigs.First().Value;

        var selectedConfig = AnsiConsole.Prompt(
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
            AnsiConsole.MarkupLine($"[yellow]Warning: Unable to find RDS template '{dbConfig.Template}'[/]");

            return;
        }

        dbConfig.Hostname ??= templateConfig.Hostname;
        dbConfig.Port ??= templateConfig.Port;
        dbConfig.Username ??= templateConfig.Username;
        dbConfig.Region ??= templateConfig.Region;
        dbConfig.Ttl ??= templateConfig.Ttl;
    }
}
