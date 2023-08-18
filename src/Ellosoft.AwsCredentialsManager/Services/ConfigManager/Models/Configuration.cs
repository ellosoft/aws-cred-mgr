namespace Ellosoft.AwsCredentialsManager.Services.ConfigManager.Models;

public class VariableConfiguration
{
    public Dictionary<string, object>? Variables { get; set; }
}

public class Configuration
{
    public Authentication? Authentication { get; set; }

    public Templates? Templates { get; set; }

    public Dictionary<string, Environment>? Environments { get; set; }
}

public class Authentication
{
    public Dictionary<string, OktaConfiguration>? Okta { get; set; }
}

public class Templates
{
    public Dictionary<string, DatabaseConfiguration>? Rds { get; set; }
}

public class Environment
{
    public string Auth { get; set; } = String.Empty;

    public Dictionary<string, DatabaseConfiguration>? Rds { get; set; }
}
