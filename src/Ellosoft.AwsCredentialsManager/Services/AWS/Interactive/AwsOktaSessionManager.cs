// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
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

    public async Task<AWSCredentials?> CreateOrResumeSessionAsync(string credentialProfile)
    {
        if (!TryGetCredential(credentialProfile, out var credentialsConfig))
            return null;

        if (TryResumeSession(credentialsConfig.AwsProfile, out var awsCredentialsData) && awsCredentialsData.RoleArn == credentialsConfig.RoleArn)
            return CreateAwsCredentials(awsCredentialsData);

        var newCredential = await CreateSessionAsync(credentialProfile, credentialsConfig);

        return newCredential is not null ? CreateAwsCredentials(newCredential) : null;
    }

    private bool TryGetCredential(string credentialProfile, [NotNullWhen(true)] out CredentialsConfiguration? credentialsConfig)
    {
        if (_configManager.AppConfig.Credentials.TryGetValue(credentialProfile, out credentialsConfig))
        {
            if (credentialsConfig is { OktaProfile: not null, OktaAppUrl: not null })
                return true;

            AnsiConsole.MarkupLine($"[yellow]The credential [b]'{credentialProfile}'[/] has invalid Okta properties[/]");
        }

        AnsiConsole.MarkupLine($"[yellow]Unable to find credential [b]'{credentialProfile}'[/][/]");

        return false;
    }

    private bool TryResumeSession(string? awsProfile, [NotNullWhen(true)] out AwsCredentialsData? credentialsData)
    {
        credentialsData = _awsCredentialsService.GetCredentialsFromStore(awsProfile ?? "default");

        if (credentialsData is null)
            return false;

        if (credentialsData.ExpirationDateTime >= DateTime.Now.AddMinutes(60))
            return true;

        var expirationInMinutes = (int)(credentialsData.ExpirationDateTime - DateTime.Now).TotalMinutes;

        var renewCredentialsMessage = $"""
                                       [bold yellow]Your AWS credentials will expire in [bold green]{expirationInMinutes}[/] minutes.
                                       Any tokens (RDS password, PreSigned URLs, etc) created with it will also expired within that time frame.
                                       Do you want renew the credentials now ?[/]
                                       """;

        if (AnsiConsole.Confirm(renewCredentialsMessage, defaultValue: false))
            return false;

        return true;
    }

    private async Task<AwsCredentialsData?> CreateSessionAsync(string credentialProfile, CredentialsConfiguration credentialsConfig)
    {
        var authResult = await _oktaLoginService.InteractiveLogin(credentialsConfig.OktaProfile!);

        if (authResult?.SessionToken is null)
            return null;

        var samlData = await _oktaSamlService.GetAppSamlDataAsync(authResult.OktaDomain, credentialsConfig.OktaAppUrl!, authResult.SessionToken);

        var idp = GetRoleIdp(credentialProfile, credentialsConfig.RoleArn, samlData.SamlAssertion);

        if (idp is null)
            return null;

        var awsCredentialsData = await _awsCredentialsService.GetAwsCredentials(samlData.SamlAssertion, credentialsConfig.RoleArn, idp);

        _awsCredentialsService.StoreCredentials(credentialsConfig.AwsProfile, awsCredentialsData);

        return awsCredentialsData;
    }

    private string? GetRoleIdp(string credentialProfile, string roleArn, string samlAssertion)
    {
        var roles = _awsSamlService.GetAwsRolesAndIdpFromSamlAssertion(samlAssertion);

        if (roles.TryGetValue(roleArn, out var idp))
            return idp;

        var invalidCredentialMessage = $"""
                                        [bold yellow]The AWS role ARN specified in the credential [b]'{credentialProfile}'[/] is not assigned to your user.
                                        Please update the [b]'{credentialProfile}'[/] credential, with one of the following roles:[/]
                                        """;

        AnsiConsole.MarkupLine(invalidCredentialMessage);

        foreach (var (role, _) in roles)
            AnsiConsole.MarkupLine(role);

        return null;
    }

    private static BasicAWSCredentials CreateAwsCredentials(AwsCredentialsData credentialsData) =>
        new(credentialsData.AccessKeyId, credentialsData.SecretAccessKey);
}
