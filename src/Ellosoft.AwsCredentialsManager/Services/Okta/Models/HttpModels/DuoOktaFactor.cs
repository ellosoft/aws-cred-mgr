// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class DuoOktaFactor : OktaFactor
{
    [JsonPropertyName("_embedded")]
    public DuoOktaFactorDetails? Embedded { get; set; }

    public class DuoOktaFactorDetails
    {
        public required DuoVerification Verification { get; set; }
    }

    public class DuoVerification
    {
        public string? Host { get; set; }

        public string? Signature { get; set; }

        public string? FactorResult { get; set; }

        [JsonPropertyName("_links")]
        public Dictionary<string, Link>? Links { get; set; }
    }
}
