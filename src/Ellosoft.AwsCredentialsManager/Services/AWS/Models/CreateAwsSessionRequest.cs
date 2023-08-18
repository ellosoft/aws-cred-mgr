// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Amazon;

namespace Ellosoft.AwsCredentialsManager.Services.AWS.Models;

public record CreateOktaAwsSessionRequest
{
    public required string OktaDomain { get; set; }

    public required string AwsAppLink { get; set; }

    public required string RoleArn { get; set; }

    public required RegionEndpoint Region { get; set; }

    public string? PreferredMfaType { get; set; }

    public string AwsProfile { get; set; } = "default";

    public string UserProfileKey { get; set; } = "default";
}
