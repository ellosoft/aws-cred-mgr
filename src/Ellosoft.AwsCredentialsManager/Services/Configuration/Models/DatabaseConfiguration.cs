// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class DatabaseConfiguration : ResourceConfiguration
{
    public string? Hostname { get; set; }

    public int? Port { get; set; }

    public string? Username { get; set; }

    public int? Ttl { get; set; }

    public string? Region { get; set; }

    public string? Template { get; set; }

    public string? Credential { get; set; }
}
