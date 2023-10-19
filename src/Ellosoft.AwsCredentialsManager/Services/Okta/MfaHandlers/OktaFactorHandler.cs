// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public abstract class OktaFactorHandler : IOktaMfaHandler
{
    private readonly HttpClient _httpClient;

    protected OktaFactorHandler(HttpClient httpClient) => _httpClient = httpClient;

    public abstract Task<Oa> VerifyFactor(Uri oktaDomain, Factor factor, string stateToken);

    protected async Task<Fa> VerifyFactorAsync<T>(Uri oktaDomain, string factorId, T request, JsonTypeInfo<T> jsonTypeInfo)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync(new Uri(oktaDomain, $"/api/v1/authn/factors/{factorId}/verify"), request, jsonTypeInfo);

        return await httpResponse.Content.ReadFromJsonAsync(SourceGenerationContext.Default.AuthenticationResponse) ?? throw new InvalidOperationException();
    }
}
