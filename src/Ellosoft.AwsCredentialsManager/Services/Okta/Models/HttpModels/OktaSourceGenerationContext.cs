// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]

[JsonSerializable(typeof(AuthenticationRequest))]
[JsonSerializable(typeof(AuthenticationResponse))]

[JsonSerializable(typeof(OktaApiError))]
[JsonSerializable(typeof(OktaFactor))]

[JsonSerializable(typeof(FactorVerificationResponse<DuoOktaFactor>))]
[JsonSerializable(typeof(FactorVerificationResponse<PushOktaFactor>))]
[JsonSerializable(typeof(FactorVerificationResponse<object>))]
internal partial class OktaSourceGenerationContext : JsonSerializerContext
{
}
