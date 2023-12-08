// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class CredentialsConfiguration : ResourceConfiguration
{
    public required string RoleArn { get; set; }

    public string? AwsProfile { get; set; }

    public string? OktaAppUrl { get; set; }

    public string? OktaProfile { get; set; }

    /// <summary>
    ///     If the AwsProfile is populated then return its value otherwise returns the credential name
    /// </summary>
    internal string GetAwsProfileSafe(string credentialName) => AwsProfile ?? credentialName;
}
