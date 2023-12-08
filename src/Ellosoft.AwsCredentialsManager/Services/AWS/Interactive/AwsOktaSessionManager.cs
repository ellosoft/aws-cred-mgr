// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Amazon.Runtime;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;

public class AwsOktaSessionManager(
    CredentialsManager credentialsManager,
    IOktaLoginService loginService,
    OktaSamlService oktaSamlService)
{
    private readonly AwsCredentialsService _awsCredentialsService = new();
    private readonly AwsSamlService _awsSamlService = new();

    public async Task<AWSCredentials?> CreateOrResumeSessionAsync(string credentialProfile, string? outputAwsProfile)
    {
        if (!credentialsManager.TryGetCredential(credentialProfile, out var credentialsConfig))
            return null;

        var awsProfile = credentialsConfig.GetAwsProfileSafe(credentialProfile);

        if (TryResumeSession(awsProfile, credentialsConfig.RoleArn, out var awsCredentialsData))
            return CreateAwsCredentials(awsCredentialsData, awsProfile, outputAwsProfile);

        var newCredential = await CreateSessionAsync(credentialProfile, awsProfile, credentialsConfig);

        return newCredential is not null ? CreateAwsCredentials(newCredential, awsProfile, outputAwsProfile) : null;
    }

    private bool TryResumeSession(string awsProfile, string roleArn, [NotNullWhen(true)] out AwsCredentialsData? credentialsData)
    {
        credentialsData = _awsCredentialsService.GetCredentialsFromStore(awsProfile);

        if (credentialsData is null || credentialsData.RoleArn != roleArn)
            return false;

        if (credentialsData.ExpirationDateTime >= DateTime.Now.AddMinutes(60))
            return true;

        var expirationInMinutes = (int)(credentialsData.ExpirationDateTime - DateTime.Now).TotalMinutes;

        var renewCredentialsMessage = $"""
                                       [bold yellow]Your AWS credentials will expire in [bold green]{expirationInMinutes}[/] minutes.
                                       Any tokens (RDS password, PreSigned URLs, etc) created with it will also expired within that time frame.
                                       Do you want renew the credentials now ?[/]
                                       """;

        AnsiConsole.WriteLine();

        if (AnsiConsole.Confirm(renewCredentialsMessage, defaultValue: false))
            return false;

        return true;
    }

    private async Task<AwsCredentialsData?> CreateSessionAsync(string credentialProfile, string awsProfile, CredentialsConfiguration credentialsConfig)
    {
        var authResult = await loginService.InteractiveLogin(credentialsConfig.OktaProfile!);

        if (authResult?.SessionToken is null)
            return null;

        var samlData = await oktaSamlService.GetAppSamlDataAsync(authResult.OktaDomain, credentialsConfig.OktaAppUrl!, authResult.SessionToken);

        var idp = GetRoleIdp(credentialProfile, credentialsConfig.RoleArn, samlData.SamlAssertion);

        if (idp is null)
            return null;

        var awsCredentialsData = await _awsCredentialsService.GetAwsCredentials(samlData.SamlAssertion, credentialsConfig.RoleArn, idp);

        _awsCredentialsService.StoreCredentials(awsProfile, awsCredentialsData);

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

    private SessionAWSCredentials CreateAwsCredentials(AwsCredentialsData credentialsData, string credentialProfile, string? outputAwsProfile)
    {
        if (outputAwsProfile is not null && outputAwsProfile != credentialProfile)
        {
            _awsCredentialsService.StoreCredentials(outputAwsProfile, credentialsData);
        }

        return new SessionAWSCredentials(credentialsData.AccessKeyId, credentialsData.SecretAccessKey, credentialsData.SessionToken);
    }
}
