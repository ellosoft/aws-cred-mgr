// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;

public interface IEnvironmentManager
{
    EnvironmentConfiguration GetOrCreateEnvironment(string? environment);

    EnvironmentConfiguration? GetEnvironment(string environment);
}

public class EnvironmentManager(IConfigManager configManager, ICredentialsManager credentialsManager) : IEnvironmentManager
{
    private sealed record Environment(string EnvironmentName, EnvironmentConfiguration? Configuration);

    public EnvironmentConfiguration GetOrCreateEnvironment(string? environment)
    {
        var appConfig = configManager.AppConfig;

        if (environment is not null && appConfig.Environments.TryGetValue(environment, out var existingEnv))
            return existingEnv;

        if (environment is not null || appConfig.Environments.Count == 0)
            return CreateNewEnvironment(environment);

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

    public EnvironmentConfiguration? GetEnvironment(string environment) =>
        configManager.AppConfig.Environments.GetValueOrDefault(environment);

    private EnvironmentConfiguration CreateNewEnvironment(string? environment = null)
    {
        var environmentName = environment ?? AnsiConsole.Ask<string>("Enter the environment name:");

        AnsiConsole.MarkupLine($"Creating environment [green i]{environmentName}[/]");

        var credentialName = credentialsManager.GetCredentialNameFromUser();
        var environmentConfig = new EnvironmentConfiguration { Credential = credentialName };

        while (!configManager.AppConfig.Environments.TryAdd(environmentName, environmentConfig))
        {
            environmentName = AnsiConsole.Ask<string>("This environment already exists, please choose another name:");
        }

        configManager.SaveConfig();

        AnsiConsole.MarkupLine("Environment created");

        return environmentConfig;
    }
}
