// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Amazon;
using Ellosoft.AwsCredentialsManager.Services.Okta;

namespace Ellosoft.AwsCredentialsManager.Services.AWS.Models;

public record CreateOktaAwsSessionRequest
{
    public required RegionEndpoint Region { get; set; }

    public string AwsProfile { get; set; } = "default";

    public string OktaUserProfile { get; set; } = OktaConstants.DefaultProfileName;

    public string? PreferredMfaType { get; set; }
}
