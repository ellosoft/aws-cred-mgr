// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public static class FactorResult
{
    public const string Waiting = "WAITING";
    public const string Challenge = "CHALLENGE";
    public const string Timeout = "TIMEOUT";
    public const string Error = "ERROR";
    public const string Cancelled = "CANCELLED";

    // non-official Okta statuses
    public const string Unauthorized = "UNAUTHORIZED";
}
