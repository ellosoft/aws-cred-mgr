// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.AWS.Models;

namespace Ellosoft.AwsCredentialsManager.Services.AWS;

public class AwsOktaSessionManager
{
    //private readonly IOktaLoginService _oktaLoginService;
    //private readonly IConfigManager _configManager;

    private readonly AwsCredentialsService _awsCredentialsService = new();
    //private readonly AwsSamlService _awsSamlService = new();
    //private readonly OktaClassicAuthenticator _oktaAuth = new();

    //public AwsOktaSessionManager(IOktaLoginService loginService, IConfigManager configManager)
    //{
    //    _oktaLoginService = loginService;
    //    _configManager = configManager;
    //}

    public Task<bool> CreateSession(CreateOktaAwsSessionRequest request)
    {
        //var appSettings = _configManager.AppConfig.

        //var sessionToken = await _oktaLoginService.InteractiveLogin(request.OktaUserProfile);

        //if (sessionToken is null)
        //    return false;

        //var samlData = await _oktaAuth.GetAppSamlData(request.OktaDomain, sessionToken, request.AwsAppLink);

        //var roles = _awsSamlService.GetAwsRolesAndIdpFromSamlAssertion(samlData.SamlAssertion);

        //var idp = roles[request.RoleArn];
        //var credentials = await _awsCredentialsService.GetAwsCredentials(samlData.SamlAssertion, request.RoleArn, idp, request.Region);

        //_awsCredentialsService.StoreCredentials(request.AwsProfile, credentials, request.Region);

        //return true;

        throw new NotImplementedException();
    }

    public bool TryResumeSession(string awsProfile, out AwsCredentialsData? credentials)
    {
        credentials = _awsCredentialsService.GetCredentialsFromStore(awsProfile);

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
}
