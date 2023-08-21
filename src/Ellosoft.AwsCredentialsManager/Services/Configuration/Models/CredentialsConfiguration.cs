// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class CredentialsConfiguration : ResourceConfiguration
{
    public string? RoleArn { get; set; }

    public string? AwsProfile { get; set; }

    public string? Region { get; set; }

    public string? OktaAppLink { get; set; }

    public string? OktaProfile { get; set; }
}
