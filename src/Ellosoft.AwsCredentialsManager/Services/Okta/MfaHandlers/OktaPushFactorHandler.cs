// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

using static Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels.OktaSourceGenerationContext;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaPushFactorHandler : OktaFactorHandler
{
    public OktaPushFactorHandler(HttpClient httpClient) : base(httpClient)
    {
    }

    public override async Task<FactorVerificationResponse> VerifyFactorAsync(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        var verifyFactorRequest = new VerifyPushFactorRequest { StateToken = stateToken };

        var mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, Default.VerifyPushFactorRequest, Default.FactorVerificationResponsePushOktaFactor);

        var factorResult = mfaAuthResponse.FactorResult;

        AnsiConsole.WriteLine("Okta push sent... Please check your phone");
        AnsiConsole.WriteLine("Waiting response...");

        while (factorResult == FactorResult.Waiting)
        {
            await Task.Delay(2000);

            mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, Default.VerifyPushFactorRequest, Default.FactorVerificationResponsePushOktaFactor);
            factorResult = mfaAuthResponse.FactorResult;

            Verify3NumberPushMfaChallenge(mfaAuthResponse);
        }

        return mfaAuthResponse;
    }

    private static void Verify3NumberPushMfaChallenge(FactorVerificationResponse<PushOktaFactor> authResponse)
    {
        var factorChallenge = authResponse.Embedded.Factor?.Embedded?.Challenge.CorrectAnswer;

        if (factorChallenge is not null)
        {
            AnsiConsole.MarkupLine(
                $"[yellow][[Extra Verification Required]][/] Please select the following number in your Okta Verify App: [bold yellow]{factorChallenge}[/]");
        }
    }
}
