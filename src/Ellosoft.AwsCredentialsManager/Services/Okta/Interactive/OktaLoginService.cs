// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

public interface IOktaLoginService
{
    Task<string?> InteractiveLogin(string userProfileKey);

    Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = "default");
}

public class OktaLoginService : IOktaLoginService
{
    private readonly IConfigManager _configManager;
    private static readonly bool RunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private readonly UserCredentialsManager _userCredentialsManager = new();
    private readonly OktaClassicAuthenticator _oktaAuth = new();

    public OktaLoginService(IConfigManager configManager) => _configManager = configManager;

    public async Task<string?> InteractiveLogin(string userProfileKey)
    {
        var userCredentials = GetUserCredentials(userProfileKey, out var savedCredentials);

        if (TryGetOktaConfig(userProfileKey, out var oktaConfig))
        {
            var authResult = await Login(new Uri(oktaConfig.OktaDomain!), userCredentials, oktaConfig.PreferredMfaType, savedCredentials, userProfileKey);

            return authResult.SessionToken;
        }

        AnsiConsole.MarkupLine($"[red]No '{userProfileKey}' Okta profile found, please use [green]okta setup[/] to create a new profile[/]");

        return null;
    }

    public async Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = "default")
    {
        try
        {
            var authResult = await _oktaAuth.Authenticate(oktaDomain, userCredentials.Username, userCredentials.Password, preferredMfaType);

            SaveUserCredentials(userProfileKey, userCredentials, savedCredentials);

            return authResult;
        }
        catch (Exception e) when (e is InvalidUsernameOrPasswordException or PasswordExpiredException)
        {
            ClearStoredPassword(userProfileKey, userCredentials);

            return new AuthenticationResult { OktaDomain = oktaDomain, Authenticated = false };
        }
    }

    private void SaveUserCredentials(string userProfileKey, UserCredentials userCredentials, bool savedCredentials)
    {
        if (!RunningOnWindows) // this will get removed once the Keychain support is implemented
            return;

        if (savedCredentials)
            return;

        if (AnsiConsole.Confirm("Do you want to save your Okta username and password for future logins ?"))
        {
            _userCredentialsManager.SaveUserCredentials(userProfileKey, userCredentials);

            return;
        }

        AnsiConsole.MarkupLine("[yellow]Ok... :([/]");
    }

    private UserCredentials GetUserCredentials(string userProfileKey, out bool savedCredentials)
    {
        UserCredentials? user = null;
        savedCredentials = false;

        if (RunningOnWindows)
        {
            user = _userCredentialsManager.GetUserCredentials(userProfileKey);

            if (!string.IsNullOrWhiteSpace(user?.Password))
            {
                savedCredentials = true;

                return user;
            }
        }

        AnsiConsole.MarkupLine("Let's get you logged in !");

        var username = user?.Username is null
            ? AnsiConsole.Ask<string>("Enter your [green]Okta[/] username:")
            : AnsiConsole.Ask("Enter your [green]Okta[/] username:", user.Username);

        var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [green]Okta[/] password:").Secret());

        return new UserCredentials(username, password);
    }

    private void ClearStoredPassword(string profileKey, UserCredentials userCredentials)
    {
        if (!RunningOnWindows)
            return;

        var credentialsWithoutPasswords = userCredentials with { Password = string.Empty };
        _userCredentialsManager.SaveUserCredentials(profileKey, credentialsWithoutPasswords);
    }

    private bool TryGetOktaConfig(string userProfileKey, out OktaConfiguration oktaConfig)
    {
        if (_configManager.AppConfig.Authentication?.Okta?.TryGetValue(userProfileKey, out var config) == true)
        {
            oktaConfig = config;

            return true;
        }

        oktaConfig = new OktaConfiguration();

        return false;
    }
}
