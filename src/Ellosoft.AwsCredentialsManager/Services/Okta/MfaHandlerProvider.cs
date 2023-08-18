// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;
using Okta.Auth.Sdk;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public class MfaHandlerProvider
{
    public IOktaMfaHandler GetOktaFactorHandler(string factorType, IAuthenticationClient authClient)
    {
        return factorType switch
        {
            "push" => new OktaPushFactorHandler(authClient),
            "token:software:totp" => new OktaTotpFactorHandler(authClient),
            _ => throw new NotSupportedException($"Factor type '{factorType}' is not supported")
        };
    }
}
