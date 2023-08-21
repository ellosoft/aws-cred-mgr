// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Configuration;

public class ConfigManager
{
    private const string APP_CONFIG_FILE = "aws_cred_mgr.yaml";

    private static readonly string AppConfigPath = Path.Combine(AppDataDirectory.UserProfileDirectory, APP_CONFIG_FILE);

    private readonly ConfigReader _configParser = new();

    public AppConfig GetConfiguration() => File.Exists(AppConfigPath) ? _configParser.Read(AppConfigPath) : new AppConfig();

    public void SaveConfiguration(AppConfig config)
    {
        //var yaml = _configParser.Serialize(config);
        //File.WriteAllText(AppConfigPath, yaml);
    }
}
