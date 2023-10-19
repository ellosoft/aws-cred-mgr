using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class AuthenticationResponse
{
    public string? StateToken { get; set; }

    public string? SessionToken { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public required string Status { get; set; }

    [JsonPropertyName("_embedded")]
    public AuthenticationResponseDetails? Embedded { get; set; }

    public class AuthenticationResponseDetails
    {
        public IList<OktaFactor> Factors { get; set; } = Array.Empty<OktaFactor>();
    }
}
