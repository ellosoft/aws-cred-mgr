// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Runtime.InteropServices;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

public interface IOktaLoginService
{
    Task<string?> InteractiveLogin(string oktaDomain, string? preferredMfaType, string userProfileKey);

    Task<string?> Login(string oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = "default");
}

public class OktaLoginService : IOktaLoginService
{
    private static readonly bool RunningOnWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

    private readonly UserCredentialsManager _userCredentialsManager = new();
    private readonly OktaClassicAuthenticator _oktaAuth = new();

    public Task<string?> InteractiveLogin(string oktaDomain, string? preferredMfaType, string userProfileKey)
    {
        var userCredentials = GetUserCredentials(userProfileKey, out var savedCredentials);

        return Login(oktaDomain, userCredentials, preferredMfaType, savedCredentials, userProfileKey);
    }

    public async Task<string?> Login(string oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = "default")
    {
        try
        {
            var sessionToken = await _oktaAuth.Authenticate(oktaDomain, userCredentials.Username, userCredentials.Password, preferredMfaType);

            SaveUserCredentials(userProfileKey, userCredentials, savedCredentials);

            return sessionToken;
        }
        catch (Exception e) when (e is InvalidUsernameOrPasswordException or PasswordExpiredException)
        {
            ClearStoredPassword(userProfileKey, userCredentials);

            return null;
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
}
