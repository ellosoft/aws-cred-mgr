// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class OktaConfiguration : ResourceConfiguration
{
    public const string DefaultProfileName = "default";

    public required string OktaDomain { get; set; }

    /// <summary>
    ///     Preferred MFA method (this will be null if no MFA is required)
    /// </summary>
    public string? PreferredMfaType { get; set; }

    public string AuthType { get; set; } = "classic";
}
