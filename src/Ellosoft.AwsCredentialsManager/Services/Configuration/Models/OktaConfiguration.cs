// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class OktaConfiguration : ResourceConfiguration
{
    public string? OktaDomain { get; set; }

    public string? PreferredMfaType { get; set; }
}
