// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class CredentialsConfiguration : ResourceConfiguration
{
    public required string RoleArn { get; set; }

    public required string AwsProfile { get; set; } = "default";

    public string? OktaAppUrl { get; set; }

    public string? OktaProfile { get; set; }
}
