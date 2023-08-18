namespace Ellosoft.AwsCredentialsManager.Services.ConfigManager.Models;

public class OktaConfiguration
{
    public string? OktaDomain { get; set; }

    public string? AppLink { get; set; }

    public string? PreferredMfaType { get; set; }

    public string? RoleArn { get; set; }

    public string? Region { get; set; }

    public string? AwsProfile { get; set; }
}
