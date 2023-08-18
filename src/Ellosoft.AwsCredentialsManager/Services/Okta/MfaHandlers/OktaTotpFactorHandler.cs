// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;
using Okta.Sdk.Abstractions;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaTotpFactorHandler : IOktaMfaHandler
{
    private readonly IAuthenticationClient _authClient;

    public OktaTotpFactorHandler(IAuthenticationClient authClient) => _authClient = authClient;

    public async Task<IAuthenticationResponse?> VerifyFactor(string factorId, string stateToken)
    {
        try
        {
            var passCode = AnsiConsole.Ask<int>("Enter the code displaying on your authenticator app:");

            var verifyFactorOptions = new VerifyTotpFactorOptions
            {
                FactorId = factorId,
                StateToken = stateToken,
                PassCode = passCode.ToString()
            };

            AnsiConsole.Write("Validating... ");

            var authResponse = await _authClient.VerifyFactorAsync(verifyFactorOptions);

            AnsiConsole.MarkupLine("[green]Ok![/]");

            return authResponse;
        }
        catch (OktaApiException e) when (e.StatusCode == 403)
        {
            AnsiConsole.MarkupLine("[red]Failed![/]");
            AnsiConsole.MarkupLine("[red]Your passcode doesn't match our records. Please try again.[/]");

            return await VerifyFactor(factorId, stateToken);
        }
    }
}
