// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.ConfigManager.Models;

public class DatabaseConfiguration
{
    public string? Hostname { get; set; }

    public int Port { get; set; }

    public string? UserId { get; set; }

    public string? Database { get; set; }

    public int PasswordLifetime { get; set; }

    public string? Template { get; set; }
}
