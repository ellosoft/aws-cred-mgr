// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class AppLink
{
    [JsonPropertyName("label")]
    public required string Label { get; set; }

    [JsonPropertyName("linkUrl")]
    public required string LinkUrl { get; set; }

    [JsonPropertyName("appName")]
    public required string AppName { get; set; }
}
