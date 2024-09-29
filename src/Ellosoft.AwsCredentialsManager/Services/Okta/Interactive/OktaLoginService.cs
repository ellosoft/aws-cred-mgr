// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Ellosoft.AwsCredentialsManager.Services.Security;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

public interface IOktaLoginService
{
    /// <summary>
    ///     Execute an interactive Okta user login
    /// </summary>
    /// <param name="oktaProfile">Okta profile</param>
    /// <param name="createSession">If true, a new Okta session will be created (default: false)</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult?> InteractiveLogin(string oktaProfile, bool createSession = false);

    Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = OktaConfiguration.DefaultProfileName);
}

public class OktaLoginService(
    IAnsiConsole console,
    IConfigManager configManager,
    IUserCredentialsManager userCredentialsManager,
    IOktaClassicAuthenticator classicAuthenticator)
    : IOktaLoginService
{
    public async Task<AuthenticationResult?> InteractiveLogin(string oktaProfile, bool createSession = false)
    {
        var oktaConfig = GetOktaConfig(oktaProfile);
        var userCredentials = GetUserCredentials(oktaProfile, out var savedCredentials);
        var preferredMfa = GetOktaMfaFactorCode(oktaConfig.PreferredMfaType);

        var oktaDomain = new Uri(oktaConfig.OktaDomain);
        var authResult = await Login(oktaDomain, userCredentials, preferredMfa, savedCredentials, oktaProfile);

        if (!createSession || authResult is not { Authenticated: true, SessionToken: not null })
            return authResult;

        var sessionResult = await classicAuthenticator.CreateSessionAsync(oktaDomain, authResult.SessionToken);

        if (sessionResult is not null)
        {
            authResult = authResult with { SessionId = sessionResult.Id };
        }

        return authResult;
    }

    public async Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = OktaConfiguration.DefaultProfileName)
    {
        try
        {
            var authResult = await classicAuthenticator.AuthenticateAsync(oktaDomain, userCredentials.Username, userCredentials.Password, preferredMfaType);

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
        if (savedCredentials || !userCredentialsManager.SupportCredentialsStore)
            return;

        if (console.Confirm("Do you want to save your Okta username and password for future logins ?"))
        {
            userCredentialsManager.SaveUserCredentials(userProfileKey, userCredentials);

            return;
        }

        console.MarkupLine("[yellow]Ok... :([/]");
    }

    private UserCredentials GetUserCredentials(string userProfileKey, out bool savedCredentials)
    {
        savedCredentials = false;

        var user = userCredentialsManager.GetUserCredentials(userProfileKey);

        if (!string.IsNullOrWhiteSpace(user?.Password))
        {
            savedCredentials = true;

            return user;
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
        var credentialsWithoutPasswords = userCredentials with { Password = string.Empty };
        userCredentialsManager.SaveUserCredentials(profileKey, credentialsWithoutPasswords);
    }

    private OktaConfiguration GetOktaConfig(string profile)
    {
        if (configManager.AppConfig.Authentication?.Okta.TryGetValue(profile, out var config) == true)
            return config;

        throw new OktaProfileNotFoundException(profile);
    }

    private static string? GetOktaMfaFactorCode(string? mfaType) =>
        mfaType is not null ? OktaMfaFactorSelector.GetOktaMfaFactorCode(mfaType) : null;
}
