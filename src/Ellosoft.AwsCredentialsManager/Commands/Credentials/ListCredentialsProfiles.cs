// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("list"), Alias("ls")]
[Description("List saved credential profiles")]
[Examples("ls")]
public class ListCredentialsProfiles : Command<ListCredentialsProfiles.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandOption("--okta-profile")]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        [DefaultValue("default")]
        public string OktaUserProfile { get; set; } = OktaConfiguration.DefaultProfileName;
    }

    private readonly IConfigManager _configManager;

    public ListCredentialsProfiles(IConfigManager configManager) => _configManager = configManager;

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var credentials = _configManager.AppConfig.Credentials;

        var table = new Table()
            .Title("[green]Saved credentials[/]")
            .AddColumn("Name")
            .AddColumn("Role ARN")
            .AddColumn("AWS Profile");

        var filteredCredentials = credentials.Where(kv => kv.Value.OktaProfile == settings.OktaUserProfile);

        foreach (var credential in filteredCredentials)
            table.AddRow(credential.Key, credential.Value.RoleArn, credential.Value.AwsProfile);

        AnsiConsole.Write(table);

        return 0;
    }
}
