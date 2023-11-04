// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class OktaApiError
{
    public required string ErrorCode { get; set; }

    public string? ErrorSummary { get; set; }

    public string? ErrorLink { get; set; }

    public string? ErrorId { get; set; }

    public ErrorCause[] ErrorCauses { get; set; } = Array.Empty<ErrorCause>();

    public class ErrorCause
    {
        public required string ErrorSummary { get; set; }
    }
}
