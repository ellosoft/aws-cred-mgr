// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class DatabaseConfiguration : ResourceConfiguration
{
    internal const int DefaultTtlInMinutes = 15;

    public string? Hostname { get; set; }

    public int? Port { get; set; }

    public string? Username { get; set; }

    public string? Region { get; set; }

    public int? Ttl { get; set; }

    public string? Template { get; set; }

    public string? Credential { get; set; }

    internal int GetTtl() => Ttl ?? DefaultTtlInMinutes;
}
