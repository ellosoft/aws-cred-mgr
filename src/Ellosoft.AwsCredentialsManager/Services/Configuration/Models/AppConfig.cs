// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class VariablesSection
{
    public Dictionary<string, object>? Variables { get; set; }
}

public class AppConfig
{
    public AuthenticationSection? Authentication { get; set; }

    public Dictionary<string, CredentialsConfiguration> Credentials { get; set; } = new();

    public TemplatesSection? Templates { get; set; }

    public Dictionary<string, EnvironmentConfiguration> Environments { get; set; } = new();

    public class AuthenticationSection
    {
        public Dictionary<string, OktaConfiguration> Okta { get; set; } = new();
    }

    public class TemplatesSection
    {
        public Dictionary<string, DatabaseConfiguration> Rds { get; set; } = new();
    }

    internal VariablesSection? Variables { get; set; }
}
