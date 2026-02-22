// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using static Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels.OktaSourceGenerationContext;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaTotpFactorHandler : OktaFactorHandler
{
    public OktaTotpFactorHandler(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<FactorVerificationResponse> VerifyFactorAsync(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        while (true)
        {
            var passCode = await AnsiConsole.AskAsync<int>("Enter the code displaying on your authenticator app:");

            var verifyFactorRequest = new VerifyTotpFactorRequest
            {
                StateToken = stateToken,
                PassCode = passCode.ToString()
            };

            AnsiConsole.Write("Validating... ");

            var authResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, Default.VerifyTotpFactorRequest,
                Default.FactorVerificationResponseObject);

            if (authResponse.Status == AuthenticationStatus.Success)
            {
                AnsiConsole.MarkupLine("[green]Ok![/]");

                return authResponse;
            }

            if (authResponse.StatusCode == HttpStatusCode.Forbidden)
            {
                AnsiConsole.MarkupLine("[red]Failed![/]");
                AnsiConsole.MarkupLine("[red]Your passcode doesn't match our records. Please try again.[/]");

                continue;
            }

            return authResponse;
        }
    }
}
