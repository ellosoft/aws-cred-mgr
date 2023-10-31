// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Amazon;
using Amazon.Runtime;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
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
    private readonly AwsOktaSessionManager _awsSessionManager;
    private readonly RdsTokenGenerator _rdsTokenGenerator;

    public GetRdsPassword(
        IConfigManager configManager,
        AwsOktaSessionManager awsSessionManager,
        RdsTokenGenerator rdsTokenGenerator)
    {
        _configManager = configManager;
        _awsSessionManager = awsSessionManager;
        _rdsTokenGenerator = rdsTokenGenerator;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        if (settings.Profile is not null)
            return HandleExistingProfile(settings.Profile);

        var hostname = settings.Hostname ?? AnsiConsole.Ask<string>("Enter the DB hostname:");
        var port = settings.Port ?? AnsiConsole.Ask<int>("Enter the DB port:");
        var username = settings.Username ?? AnsiConsole.Ask<string>("Enter the DB username:");

        if (_configManager.AppConfig.Credentials is null)
            throw new CommandException("[yellow]No AWS credentials found, please use [green]'aws-cred-mgr cred new'[/] to create a new profile[/]");

        var credential = AnsiConsole.Prompt(
            new SelectionPrompt<KeyValuePair<string, CredentialsConfiguration>>()
                .Title("Select an [green]AWS credential[/]:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(kv => $"{kv.Key} - {kv.Value.RoleArn}")
                .AddChoices(_configManager.AppConfig.Credentials));

        await _awsSessionManager.CreateSessionAsync(credential.Key);

        _rdsTokenGenerator.GenerateDbPassword(new AnonymousAWSCredentials(), RegionEndpoint.EUSouth1, hostname, port, username, settings.Ttl);

        return 0;
    }

    private int HandleExistingProfile(string settingsProfile)
    {
        throw new NotImplementedException();
    }
}
