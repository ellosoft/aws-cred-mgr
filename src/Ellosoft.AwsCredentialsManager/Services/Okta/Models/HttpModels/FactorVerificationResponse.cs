using System.Net;
using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class FactorVerificationResponse<TFactor> : FactorVerificationResponse
{
    [JsonPropertyName("_embedded")]
    public required FactorVerificationResponseDetails Embedded { get; set; }

    public class FactorVerificationResponseDetails
    {
        public TFactor? Factors { get; set; }

        public TFactor? Factor { get; set; }
    }
}

public class FactorVerificationResponse
{
    public required string Status { get; set; }

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string? StateToken { get; set; }

    public string? SessionToken { get; set; }

    public string? FactorResult { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }
}
