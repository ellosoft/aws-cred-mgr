// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public interface IOktaMfaHandler
{
    Task<IAuthenticationResponse?> VerifyFactor(string factorId, string stateToken);
}
