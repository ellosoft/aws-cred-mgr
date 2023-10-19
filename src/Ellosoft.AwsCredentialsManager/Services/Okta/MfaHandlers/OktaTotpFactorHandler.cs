// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;
using Okta.Auth.Sdk.Models;
using Okta.Sdk.Abstractions;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaTotpFactorHandler : OktaFactorHandler
{
    public OktaTotpFactorHandler(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<IAuthenticationResponse> VerifyFactor(Uri oktaDomain, Factor factor, string stateToken)
    {
        try
        {
            var passCode = AnsiConsole.Ask<int>("Enter the code displaying on your authenticator app:");

            var verifyFactorRequest = new VerifyTotpFactorRequest
            {
                StateToken = stateToken,
                PassCode = passCode.ToString()
            };

            AnsiConsole.Write("Validating... ");

            var authResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, SourceGenerationContext.Default.VerifyTotpFactorRequest);

            AnsiConsole.MarkupLine("[green]Ok![/]");

            return authResponse;
        }
        catch (OktaApiException e) when (e.StatusCode == 403)
        {
            AnsiConsole.MarkupLine("[red]Failed![/]");
            AnsiConsole.MarkupLine("[red]Your passcode doesn't match our records. Please try again.[/]");

            return await VerifyFactor(oktaDomain, factor, stateToken);
        }
    }
}
