// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Okta.Auth.Sdk;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public static class OktaAuthExtensions
{
    public static FactorResult GetFactorResult(this IAuthenticationResponse authResponse)
        => authResponse.GetProperty<FactorResult>("factorResult");
}
