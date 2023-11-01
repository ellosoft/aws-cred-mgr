// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class EnvironmentConfiguration : ResourceConfiguration
{
    public required string Credential { get; set; }

    public Dictionary<string, DatabaseConfiguration> Rds { get; set; } = new();
}
