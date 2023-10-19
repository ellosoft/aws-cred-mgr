// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Okta.Auth.Sdk;
using Okta.Auth.Sdk.Models;
using Okta.Sdk.Abstractions;

namespace Ellosoft.AwsCredentialsManager.Services;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(AwsCredentialsService.ProfileMetadata))]
[JsonSerializable(typeof(UserCredentials))]
[JsonSerializable(typeof(AuthenticationRequest))]
[JsonSerializable(typeof(AuthenticationResponse))]
[JsonSerializable(typeof(ApiError))]
[JsonSerializable(typeof(VerifyPushFactorRequest))]
[JsonSerializable(typeof(VerifyTotpFactorRequest))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
