// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;

public class EnvironmentManager
{
    private sealed record Environment(string EnvironmentName, EnvironmentConfiguration? Configuration);

    private readonly IConfigManager _configManager;
    private readonly CredentialsManager _credentialsManager;

    public EnvironmentManager(IConfigManager configManager, CredentialsManager credentialsManager)
    {
        _configManager = configManager;
        _credentialsManager = credentialsManager;
    }

    public EnvironmentConfiguration GetEnvironment()
    {
        var appConfig = _configManager.AppConfig;

        if (appConfig.Environments.Count == 0)
            return CreateNewEnvironment();

        var environmentOptions = appConfig.Environments
            .OrderBy(kv => kv.Key)
            .Select(kv => new Environment(kv.Key, kv.Value))
            .ToList();

        environmentOptions.Add(new Environment("[green][[New Environment]][/]", null));

        var selectedEnv = AnsiConsole.Prompt(
            new SelectionPrompt<Environment>()
                .Title("Select an environment:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(env => env.EnvironmentName)
                .AddChoices(environmentOptions));

        return selectedEnv.Configuration ?? CreateNewEnvironment();
    }

    private EnvironmentConfiguration CreateNewEnvironment()
    {
        var environmentName = AnsiConsole.Ask<string>("Enter the environment name:");

        var credentialName = _credentialsManager.GetCredential();
        var environmentConfig = new EnvironmentConfiguration { Credential = credentialName };

        while (!_configManager.AppConfig.Environments.TryAdd(environmentName, environmentConfig))
        {
            environmentName = AnsiConsole.Ask<string>("This environment already exists, please choose another name:");
        }

        _configManager.SaveConfig();

        return environmentConfig;
    }
}
