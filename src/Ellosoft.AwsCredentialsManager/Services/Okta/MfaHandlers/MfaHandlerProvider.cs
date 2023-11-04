// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public class MfaHandlerProvider
{
    public IOktaMfaHandler GetOktaFactorHandler(HttpClient httpClient, string factorType)
    {
        return factorType switch
        {
            "push" => new OktaPushFactorHandler(httpClient),
            "token:software:totp" => new OktaTotpFactorHandler(httpClient),
            _ => throw new NotSupportedException($"MFA type '{factorType}' is not yet supported")
        };
    }
}
