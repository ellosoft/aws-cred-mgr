using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class PushOktaFactor : OktaFactor
{
    [JsonPropertyName("_embedded")]
    public PushOktaFactorDetails? Embedded { get; set; }

    public class PushOktaFactorDetails
    {
        public PushChallenge? Challenge { get; set; }
    }

    public class PushChallenge
    {
        public long CorrectAnswer { get; set; }
    }
}