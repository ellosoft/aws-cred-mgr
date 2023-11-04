// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AuthenticationRequest))]
[JsonSerializable(typeof(AuthenticationResponse))]
[JsonSerializable(typeof(VerifyPushFactorRequest))]
[JsonSerializable(typeof(FactorVerificationResponse<PushOktaFactor>))]
[JsonSerializable(typeof(FactorVerificationResponse<DuoOktaFactor>))]
[JsonSerializable(typeof(VerifyTotpFactorRequest))]
[JsonSerializable(typeof(FactorVerificationResponse<object>))]
[JsonSerializable(typeof(OktaApiError))]
[JsonSerializable(typeof(TokenResponse))]
[JsonSerializable(typeof(List<AppLink>))]
internal partial class OktaSourceGenerationContext : JsonSerializerContext
{
}
