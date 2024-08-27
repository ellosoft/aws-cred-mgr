// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("list"), Alias("ls")]
[Description("List saved credential profiles")]
[Examples("ls")]
public class ListCredentialsProfiles(IConfigManager configManager) : Command<ListCredentialsProfiles.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandOption("--okta-profile")]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        [DefaultValue("default")]
        public string OktaUserProfile { get; set; } = OktaConfiguration.DefaultProfileName;
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        var credentials = configManager.AppConfig.Credentials;

        var table = new Table()
            .Title("[green]Saved credentials[/]")
            .AddColumn("Name")
            .AddColumn("Role ARN")
            .AddColumn("AWS Profile");

        var filteredCredentials = credentials.Where(kv => kv.Value.OktaProfile == settings.OktaUserProfile);

        foreach (var (credentialName, credentialConfig) in filteredCredentials)
            table.AddRow(credentialName, credentialConfig.RoleArn, credentialConfig.GetAwsProfileSafe(credentialName));

        AnsiConsole.Write(table);

        return 0;
    }
}
