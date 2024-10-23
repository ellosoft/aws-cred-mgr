// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;
using static Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels.OktaSourceGenerationContext;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaPushFactorHandler(HttpClient httpClient) : OktaFactorHandler(httpClient)
{
    public override async Task<FactorVerificationResponse> VerifyFactorAsync(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        var verifyFactorRequest = new VerifyPushFactorRequest { StateToken = stateToken };

        var mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, Default.VerifyPushFactorRequest,
            Default.FactorVerificationResponsePushOktaFactor);

        var factorResult = mfaAuthResponse.FactorResult;
        var challengeMsgShown = false;

        AnsiConsole.WriteLine("Okta push sent... Please check your phone");
        AnsiConsole.WriteLine("Waiting response...");

        while (factorResult == FactorResult.Waiting)
        {
            await Task.Delay(2_000);

            mfaAuthResponse = await VerifyFactorAsync(oktaDomain, factor.Id, verifyFactorRequest, Default.VerifyPushFactorRequest,
                Default.FactorVerificationResponsePushOktaFactor);

            factorResult = mfaAuthResponse.FactorResult;

            if (!challengeMsgShown && ChallengeRequired(mfaAuthResponse, out var factorChallenge))
            {
                Show3NumberPushMfaChallengeMessage(factorChallenge);
                challengeMsgShown = true;
            }
        }

        return mfaAuthResponse;
    }

    private static void Show3NumberPushMfaChallengeMessage(long factorChallenge)
    {
        AnsiConsole.MarkupLine(
            $"[yellow][[Extra Verification Required]][/] Please select the following number in your Okta Verify App: [bold yellow]{factorChallenge}[/]");
    }

    private static bool ChallengeRequired(FactorVerificationResponse<PushOktaFactor> authResponse, out long factorChallenge)
    {
        var correctAnswer = authResponse.Embedded.Factor?.Embedded?.Challenge.CorrectAnswer;
        if (correctAnswer is not null)
        {
            factorChallenge = correctAnswer.Value;
            return true;
        }

        factorChallenge = 0;
        return false;
    }
}
