// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models;

public record AuthenticationResult
{
    public required Uri OktaDomain { get; init; }

    public string? MfaUsed { get; init; }

    public string? StateToken { get; init; }

    public string? SessionToken { get; init; }

    public bool Authenticated { get; init; }
}
