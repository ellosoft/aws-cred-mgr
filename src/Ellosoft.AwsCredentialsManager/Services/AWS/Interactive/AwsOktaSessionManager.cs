// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Services.AWS.Models;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;

public class AwsOktaSessionManager
{
    private readonly IConfigManager _configManager;
    private readonly IOktaLoginService _oktaLoginService;
    private readonly OktaSamlService _oktaSamlService;

    private readonly AwsCredentialsService _awsCredentialsService = new();
    private readonly AwsSamlService _awsSamlService = new();

    public AwsOktaSessionManager(
        IConfigManager configManager,
        IOktaLoginService loginService,
        OktaSamlService oktaSamlService)
    {
        _configManager = configManager;
        _oktaLoginService = loginService;
        _oktaSamlService = oktaSamlService;
    }

    public async Task<bool> CreateSessionAsync(string credentialKey)
    {
        if (!TryGetCredential(credentialKey, out var credentialsConfig))
            return false;

        var authResult = await _oktaLoginService.InteractiveLogin(credentialsConfig.OktaProfile!);

        if (authResult?.SessionToken is null)
            return false;

        var samlData = await _oktaSamlService.GetAppSamlDataAsync(authResult.OktaDomain, credentialsConfig.OktaAppUrl!, authResult.SessionToken);

        var idp = GetRoleIdp(credentialKey, credentialsConfig.RoleArn, samlData.SamlAssertion);

        if (idp is null)
            return false;

        var awsCredentialsData = await _awsCredentialsService.GetAwsCredentials(samlData.SamlAssertion, credentialsConfig.RoleArn, idp);

        _awsCredentialsService.StoreCredentials(credentialsConfig.AwsProfile, awsCredentialsData);

        return true;
    }

    public bool TryResumeSession(string? awsProfile, out AwsCredentialsData? credentials)
    {
        credentials = _awsCredentialsService.GetCredentialsFromStore(awsProfile ?? "default");

        if (credentials is null)
            return false;

        if (credentials.ExpirationDateTime >= DateTime.Now.AddMinutes(60))
            return true;

        var expirationInMinutes = (int)(credentials.ExpirationDateTime - DateTime.Now).TotalMinutes;

        var renewCredentialsMessage = $"""
                                       [bold yellow]Your AWS credentials will expire in [bold green]{expirationInMinutes}[/] minutes.
                                       Any tokens (RDS password, PreSigned URLs, etc) created with it will also expired within that time frame.
                                       Do you want renew the credentials now ?[/]
                                       """;

        if (AnsiConsole.Confirm(renewCredentialsMessage, defaultValue: false))
            return false;

        return true;
    }

    private bool TryGetCredential(string credentialKey, [NotNullWhen(true)] out CredentialsConfiguration? credentialsConfig)
    {
        credentialsConfig = null;

        if (_configManager.AppConfig.Credentials?.TryGetValue(credentialKey, out credentialsConfig) == true)
        {
            if (credentialsConfig is { OktaProfile: not null, OktaAppUrl: not null })
                return true;

            AnsiConsole.MarkupLine($"[yellow]The credential [b]'{credentialKey}'[/] has invalid Okta properties[/]");
        }

        AnsiConsole.MarkupLine($"[yellow]Unable to find credential [b]'{credentialKey}'[/][/]");

        return false;
    }

    private string? GetRoleIdp(string credentialKey, string roleArn, string samlAssertion)
    {
        var roles = _awsSamlService.GetAwsRolesAndIdpFromSamlAssertion(samlAssertion);

        if (roles.TryGetValue(roleArn, out var idp))
            return idp;

        var invalidCredentialMessage = $"""
                                        [bold yellow]The AWS role ARN specified in the credential [b]'{credentialKey}'[/] is not assigned to your user.
                                        Please update the [b]'{credentialKey}'[/], with one of the following roles:[/]
                                        """;

        AnsiConsole.MarkupLine(invalidCredentialMessage);

        foreach (var (role, _) in roles)
            AnsiConsole.MarkupLine(role);

        return null;
    }
}
