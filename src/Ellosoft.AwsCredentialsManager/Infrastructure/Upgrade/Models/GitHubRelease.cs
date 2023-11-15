// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade.Models;

public class GitHubRelease
{
    [JsonPropertyName("name")]
    public required string Name { get; set; }

    [JsonPropertyName("html_url")]
    public required string Url { get; set; }

    [JsonPropertyName("prerelease")]
    public bool PreRelease { get; set; }

    [JsonPropertyName("assets")]
    public ICollection<ReleaseAsset> Assets { get; set; } = [];

    public class ReleaseAsset
    {
        [JsonPropertyName("browser_download_url")]
        public required string DownloadUrl { get; set; }
    }
}
