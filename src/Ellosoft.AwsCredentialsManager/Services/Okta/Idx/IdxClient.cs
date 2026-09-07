// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

/// <summary>
///     Minimal Okta Identity Engine (IDX) API client bound to one sign-in transaction (cookie aware HttpClient)
/// </summary>
public sealed class IdxClient(HttpClient httpClient) : IDisposable
{
    private const string ION_JSON_MEDIA_TYPE = "application/ion+json";
    private const string OKTA_VERSION = "1.0.0";

    public HttpClient HttpClient { get; } = httpClient;

    public Task<IdxResponse> PostAsync(string href, string stateHandle, CancellationToken cancellationToken = default) =>
        PostAsync(href, new JsonObject { ["stateHandle"] = stateHandle }, cancellationToken);

    public async Task<IdxResponse> PostAsync(string href, JsonObject body, CancellationToken cancellationToken = default)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, href);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(ION_JSON_MEDIA_TYPE) { Parameters = { OktaVersionParameter() } });
        request.Content = new StringContent(body.ToJsonString(), Encoding.UTF8);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue(ION_JSON_MEDIA_TYPE) { Parameters = { OktaVersionParameter() } };

        using var response = await HttpClient.SendAsync(request, cancellationToken);

        var content = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!content.TrimStart().StartsWith('{'))
        {
            throw new OktaFastPassException(
                $"Unexpected response from Okta Identity Engine ({(int)response.StatusCode} {response.StatusCode}) when calling {request.RequestUri?.AbsolutePath}");
        }

        return IdxResponse.Parse(content);
    }

    public void Dispose() => HttpClient.Dispose();

    private static NameValueHeaderValue OktaVersionParameter() => new("okta-version", OKTA_VERSION);
}
