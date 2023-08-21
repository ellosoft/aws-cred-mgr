namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class VariablesSection
{
    public Dictionary<string, object>? Variables { get; set; }
}

public class AppConfig
{
    public AuthenticationSection? Authentication { get; set; }

    public TemplatesSection? Templates { get; set; }

    public Dictionary<string, EnvironmentConfiguration>? Environments { get; set; }

    public Dictionary<string, CredentialsConfiguration>? Credentials { get; set; }

    public class AuthenticationSection
    {
        public Dictionary<string, OktaConfiguration>? Okta { get; set; }
    }

    public class TemplatesSection
    {
        public Dictionary<string, DatabaseConfiguration>? Rds { get; set; }
    }

    internal VariablesSection? Variables { get; set; }
}
