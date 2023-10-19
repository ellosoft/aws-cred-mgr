// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaPushFactorHandler : OktaFactorHandler
{
    public OktaPushFactorHandler(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<FactorVerificationResponse<PushOktaFactor>> VerifyFactor(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        var verifyFactorRequest = new VerifyPushFactorRequest
        {
            StateToken = stateToken
        };

        var mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, SourceGenerationContext.Default.VerifyPushFactorRequest);
        var factorResult = mfaAuthResponse.GetFactorResult();

        AnsiConsole.WriteLine("Okta push sent... Please check your phone");
        AnsiConsole.WriteLine("Waiting response...");

        while (factorResult == FactorResult.Waiting)
        {
            await Task.Delay(2000);

            mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, SourceGenerationContext.Default.VerifyPushFactorRequest);
            factorResult = mfaAuthResponse.GetFactorResult();

            Verify3NumberPushMfaChallenge(mfaAuthResponse);
        }

        return mfaAuthResponse;
    }

    private static void Verify3NumberPushMfaChallenge(IAuthenticationResponse authResponse)
    {
        var factor = authResponse.Embedded?.GetProperty<Factor>("factor");
        var correctAnswer = factor?.Embedded?
            .GetProperty<Resource>("challenge")?
            .GetProperty<string>("correctAnswer");

        if (correctAnswer is not null)
        {
            AnsiConsole.MarkupLine(
                $"[yellow][[Extra Verification Required]][/] Please select the following number in your Okta Verify App: [bold yellow]{correctAnswer}[/]");
        }
    }
}
