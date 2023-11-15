// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text.Json.Serialization;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class AuthenticationResponse
{
    public required string Status { get; set; }

    public HttpStatusCode StatusCode { get; set; } = HttpStatusCode.OK;

    public string? StateToken { get; set; }

    public string? SessionToken { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    [JsonPropertyName("_embedded")]
    public AuthenticationResponseDetails? Embedded { get; set; }

    public class AuthenticationResponseDetails
    {
        public IList<OktaFactor> Factors { get; set; } = [];
    }
}
