namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models;

public record SamlData(string SamlAssertion, string SignInUrl, string RelayState);
