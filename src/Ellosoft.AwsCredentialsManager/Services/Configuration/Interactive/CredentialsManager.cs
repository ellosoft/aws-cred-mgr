// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;

public class CredentialsManager
{
    private readonly IConfigManager _configManager;

    public CredentialsManager(IConfigManager configManager) => _configManager = configManager;

    public string GetCredential()
    {
        var appConfig = _configManager.AppConfig;

        if (appConfig.Credentials.Count == 0)
            throw new CommandException("No AWS credentials found, please use [green]'aws-cred-mgr cred new'[/] to create a new profile");

        var selectedCredential = AnsiConsole.Prompt(
            new SelectionPrompt<KeyValuePair<string, CredentialsConfiguration>>()
                .Title(
                    "Select an [green]AWS credential[/]:\r\n" +
                    "(Use [green]'aws-cred-mgr cred new'[/] to create a new credential)")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(kv => $"{kv.Key} - {kv.Value.RoleArn}")
                .AddChoices(appConfig.Credentials));

        return selectedCredential.Key;
    }

    public void CreateCredential(string name, string awsProfile, string awsRole, string oktaAppUrl, string oktaProfile)
    {
        var credential = new CredentialsConfiguration
        {
            AwsProfile = awsProfile,
            RoleArn = awsRole,
            OktaAppUrl = oktaAppUrl,
            OktaProfile = oktaProfile
        };

        _configManager.AppConfig.Credentials[name] = credential;
        _configManager.SaveConfig();
    }
}
