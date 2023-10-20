// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public interface IOktaMfaHandler
{
    Task<FactorVerificationResponse> VerifyFactorAsync(Uri oktaDomain, OktaFactor factor, string stateToken);
}
