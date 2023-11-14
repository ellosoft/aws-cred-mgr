// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade.Models;

[JsonSourceGenerationOptions]
[JsonSerializable(typeof(GitHubRelease))]
[JsonSerializable(typeof(List<GitHubRelease>))]
internal partial class GithubSourceGenerationContext : JsonSerializerContext
{
}
