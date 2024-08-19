// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public record CreateSessionResult
{
    public string Id { get; set; } = string.Empty;

    public string UserId { get; set; } = string.Empty;

    public string Login { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    public string Status { get; set; } = string.Empty;
}
