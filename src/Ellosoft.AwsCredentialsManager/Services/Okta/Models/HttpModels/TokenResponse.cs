// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public required string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public long ExpiresIn { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }
}
