// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;

public abstract class OktaFactorHandler : IOktaMfaHandler
{
    private readonly HttpClient _httpClient;

    protected OktaFactorHandler(HttpClient httpClient) => _httpClient = httpClient;

    public abstract Task<FactorVerificationResponse> VerifyFactorAsync(Uri oktaDomain, OktaFactor factor, string stateToken);

    protected async Task<FactorVerificationResponse<TResponse>> VerifyFactorAsync<TRequest, TResponse>(
        Uri oktaDomain,
        string factorId,
        TRequest request,
        JsonTypeInfo<TRequest> requestJsonTypeInfo,
        JsonTypeInfo<FactorVerificationResponse<TResponse>> responseJsonTypeInfo)
    {
        var httpResponse = await _httpClient.PostAsJsonAsync(new Uri(oktaDomain, $"/api/v1/authn/factors/{factorId}/verify"), request, requestJsonTypeInfo);

        if (httpResponse.IsSuccessStatusCode)
            return await httpResponse.Content.ReadFromJsonAsync(responseJsonTypeInfo) ?? throw new InvalidOperationException("Invalid Okta MFA verification response");

        var apiError = await httpResponse.Content.ReadFromJsonAsync(OktaSourceGenerationContext.Default.OktaApiError);

        return new FactorVerificationResponse<TResponse>
        {
            StatusCode = httpResponse.StatusCode,
            Status = apiError?.ErrorSummary ?? httpResponse.StatusCode.ToString(),
            Embedded = new FactorVerificationResponse<TResponse>.FactorVerificationResponseDetails()
        };
    }
}
