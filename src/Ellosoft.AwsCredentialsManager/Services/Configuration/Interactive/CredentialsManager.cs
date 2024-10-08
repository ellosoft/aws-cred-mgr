// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;

public interface ICredentialsManager
{
    string GetCredentialNameFromUser();

    bool TryGetCredential(string credentialProfile, [NotNullWhen(true)] out CredentialsConfiguration? credentialsConfig);

    void CreateCredential(string name, string? awsProfile, string awsRole, string oktaAppUrl, string oktaProfile);
}

public class CredentialsManager(IConfigManager configManager) : ICredentialsManager
{
    public string GetCredentialNameFromUser()
    {
        var appConfig = configManager.AppConfig;

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

    public bool TryGetCredential(string credentialProfile, [NotNullWhen(true)] out CredentialsConfiguration? credentialsConfig)
    {
        if (configManager.AppConfig.Credentials.TryGetValue(credentialProfile, out credentialsConfig))
        {
            if (credentialsConfig is { OktaProfile: not null, OktaAppUrl: not null })
                return true;

            AnsiConsole.MarkupLine($"[yellow]The credential [b]'{credentialProfile}'[/] has invalid Okta properties[/]");
        }

        AnsiConsole.MarkupLine($"[yellow]Unable to find credential [b]'{credentialProfile}'[/][/]");

        return false;
    }

    public void CreateCredential(string name, string? awsProfile, string awsRole, string oktaAppUrl, string oktaProfile)
    {
        var credential = new CredentialsConfiguration
        {
            AwsProfile = awsProfile,
            RoleArn = awsRole,
            OktaAppUrl = oktaAppUrl,
            OktaProfile = oktaProfile
        };

        configManager.AppConfig.Credentials[name] = credential;
        configManager.SaveConfig();
    }
}
