// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class VerifyTotpFactorRequest
{
    public required string StateToken { get; set; }

    public required string PassCode { get; set; }
}
