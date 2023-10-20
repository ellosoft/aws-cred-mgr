// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(AwsCredentialsService.ProfileMetadata))]
[JsonSerializable(typeof(UserCredentials))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}
