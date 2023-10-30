// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

public interface IOktaLoginService
{
    /// <summary>
    ///     Execute an interactive Okta user login
    /// </summary>
    /// <param name="userProfileKey">Okta profile key</param>
    /// <returns>Authentication result</returns>
    Task<AuthenticationResult?> InteractiveLogin(string userProfileKey);

    /// <summary>
    ///     Execute an interactive Okta user login returning an OKTA API access token as result
    /// </summary>
    /// <param name="userProfileKey">Okta profile key</param>
    /// <returns>Access token result</returns>
    Task<AccessTokenResult?> InteractiveGetAccessToken(string userProfileKey);

    Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = OktaConstants.DefaultProfileName);
}

public class OktaLoginService : IOktaLoginService
{
    private readonly IConfigManager _configManager;
    private readonly OktaClassicAuthenticator _oktaAuth;
    private readonly OktaClassicAccessTokenProvider _oktaAccessTokenProvider;

    private readonly UserCredentialsManager _userCredentialsManager = new();

    public OktaLoginService(
        IConfigManager configManager,
        OktaClassicAuthenticator classicAuthenticator,
        OktaClassicAccessTokenProvider oktaClassicAccessTokenProvider)
    {
        _configManager = configManager;
        _oktaAuth = classicAuthenticator;
        _oktaAccessTokenProvider = oktaClassicAccessTokenProvider;
    }

    public async Task<AuthenticationResult?> InteractiveLogin(string userProfileKey)
    {
        if (!TryGetOktaConfig(userProfileKey, out var oktaConfig))
            return null;

        var userCredentials = GetUserCredentials(userProfileKey, out var savedCredentials);
        var authResult = await Login(new Uri(oktaConfig.OktaDomain!), userCredentials, oktaConfig.PreferredMfaType, savedCredentials, userProfileKey);

        return authResult;
    }

    public async Task<AccessTokenResult?> InteractiveGetAccessToken(string userProfileKey)
    {
        if (!TryGetOktaConfig(userProfileKey, out var oktaConfig))
            return null;

        var userCredentials = GetUserCredentials(userProfileKey, out var savedCredentials);

        try
        {
            var authResult = await _oktaAccessTokenProvider
                .GetAccessTokenAsync(new Uri(oktaConfig.OktaDomain!), userCredentials.Username, userCredentials.Password, oktaConfig.PreferredMfaType);

            if (authResult is not null)
                SaveUserCredentials(userProfileKey, userCredentials, savedCredentials);

            return authResult;
        }
        catch (Exception e) when (e is InvalidUsernameOrPasswordException or PasswordExpiredException)
        {
            ClearStoredPassword(userProfileKey, userCredentials);

            return null;
        }
    }

    public async Task<AuthenticationResult> Login(Uri oktaDomain, UserCredentials userCredentials,
        string? preferredMfaType = null, bool savedCredentials = false, string userProfileKey = OktaConstants.DefaultProfileName)
    {
        try
        {
            var authResult = await _oktaAuth.AuthenticateAsync(oktaDomain, userCredentials.Username, userCredentials.Password, preferredMfaType);

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
        if (savedCredentials || !_userCredentialsManager.SupportCredentialsStore)
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

        if (_userCredentialsManager.SupportCredentialsStore)
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
        if (!_userCredentialsManager.SupportCredentialsStore)
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

        var profileCommand = "aws-cred-mgr okta setup" + userProfileKey == OktaConstants.DefaultProfileName ? null : $" {userProfileKey}";
        AnsiConsole.MarkupLine($"[red]No '{userProfileKey}' Okta profile found, please use [green]'{profileCommand}'[/] to create a new profile[/]");

        return false;
    }
}
