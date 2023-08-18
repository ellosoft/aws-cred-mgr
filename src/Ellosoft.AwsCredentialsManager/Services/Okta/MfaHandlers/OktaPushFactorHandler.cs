// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class OktaPushFactorHandler : IOktaMfaHandler
{
    private readonly IAuthenticationClient _authClient;

    public OktaPushFactorHandler(IAuthenticationClient authClient) => _authClient = authClient;

    public async Task<IAuthenticationResponse?> VerifyFactor(string factorId, string stateToken)
    {
        var verifyFactorOptions = new VerifyPushFactorOptions
        {
            FactorId = factorId,
            StateToken = stateToken
        };

        var mfaAuthResponse = await _authClient.VerifyFactorAsync(verifyFactorOptions);
        var factorResult = mfaAuthResponse.GetFactorResult();

        AnsiConsole.WriteLine("Okta push sent... Please check your phone");
        AnsiConsole.WriteLine("Waiting response...");

        while (factorResult == FactorResult.Waiting)
        {
            await Task.Delay(2000);

            mfaAuthResponse = await _authClient.VerifyFactorAsync(verifyFactorOptions);
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
