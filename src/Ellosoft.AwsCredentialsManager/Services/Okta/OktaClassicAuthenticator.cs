// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Net.Http.Json;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.MfaHandlers;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public class OktaClassicAuthenticator
{
    private readonly HttpClient _httpClient;
    private readonly MfaHandlerProvider _mfaHandlerProvider;
    private readonly IOktaMfaFactorSelector _mfaFactorSelector;

    public OktaClassicAuthenticator() : this(CreateHttpClientWithCookieContainer())
    {
    }

    public OktaClassicAuthenticator(HttpClient httpClient) : this(httpClient, new MfaHandlerProvider(), new OktaMfaFactorSelector())
    {
    }

    public OktaClassicAuthenticator(HttpClient httpClient, MfaHandlerProvider mfaHandlerProvider, IOktaMfaFactorSelector mfaFactorSelector)
    {
        _httpClient = httpClient;
        _mfaHandlerProvider = mfaHandlerProvider;
        _mfaFactorSelector = mfaFactorSelector;
    }

    public async Task<AuthenticationResult> AuthenticateAsync(Uri oktaDomain, string username, string password, string? preferredMfa)
    {
        AnsiConsole.MarkupLine("Authenticating...");

        var authResponse = await AuthenticateWithUsernameAndPassword(oktaDomain, username, password);

        string? mfaUsed = null;

        if (authResponse.Status == AuthenticationStatus.MfaRequired)
        {
            AnsiConsole.MarkupLine("Verifying 2FA...");
            (authResponse, mfaUsed) = await VerifyMfa(oktaDomain, authResponse, preferredMfa);

            if (authResponse is null)
                return FailedResult(oktaDomain);
        }

        if (authResponse.Status == AuthenticationStatus.Success)
        {
            AnsiConsole.MarkupLine("[bold green]Authentication successful![/]");

            return SuccessfulResult(oktaDomain, authResponse, mfaUsed);
        }

        return HandleFailedAuthenticationResponse(oktaDomain, authResponse);
    }

    private async Task<AuthenticationResponse> AuthenticateWithUsernameAndPassword(Uri oktaDomain, string username, string password)
    {
        var authUrl = new Uri(oktaDomain, "/api/v1/authn");

        var authRequest = new AuthenticationRequest
        {
            Username = username,
            Password = password
        };

        var httpResponse = await _httpClient.PostAsJsonAsync(authUrl, authRequest, OktaSourceGenerationContext.Default.AuthenticationRequest);

        if (httpResponse.IsSuccessStatusCode)
        {
            var response = await httpResponse.Content.ReadFromJsonAsync(OktaSourceGenerationContext.Default.AuthenticationResponse);

            return response ?? throw new InvalidOperationException("Unable to deserialize successful Okta auth response");
        }

        if (httpResponse.StatusCode == HttpStatusCode.Unauthorized)
            return new AuthenticationResponse { Status = AuthenticationStatus.Unauthorized };

        var apiError = await httpResponse.Content.ReadFromJsonAsync(OktaSourceGenerationContext.Default.OktaApiError);

        return new AuthenticationResponse
        {
            StatusCode = httpResponse.StatusCode,
            Status = apiError?.ErrorSummary ?? httpResponse.StatusCode.ToString()
        };
    }

    private async Task<(AuthenticationResponse?, string)> VerifyMfa(Uri oktaDomain, AuthenticationResponse authResponse, string? preferredMfaType)
    {
        var availableMfaFactors = authResponse.Embedded?.Factors;

        if (authResponse.StateToken is null || availableMfaFactors is null || availableMfaFactors.Count == 0)
            throw new InvalidOperationException("Invalid Okta MFA authentication response");

        AuthenticationResponse? factorResponse;

        if (preferredMfaType is not null)
        {
            var preferredFactor = availableMfaFactors.FirstOrDefault(f => f.FactorType == preferredMfaType);

            if (preferredFactor is not null)
            {
                factorResponse = await ExecuteMfaFactorHandler(oktaDomain, preferredFactor, authResponse.StateToken);
                return (factorResponse, preferredFactor.FactorType);
            }
        }

        var selectedFactor = _mfaFactorSelector.GetMfaFactor(preferredMfaType, availableMfaFactors);

        factorResponse = await ExecuteMfaFactorHandler(oktaDomain, selectedFactor, authResponse.StateToken);

        return (factorResponse, selectedFactor.FactorType);
    }

    private async Task<AuthenticationResponse?> ExecuteMfaFactorHandler(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        var handler = _mfaHandlerProvider.GetOktaFactorHandler(_httpClient, factor.FactorType);

        var mfaVerificationResponse = await handler.VerifyFactorAsync(oktaDomain, factor, stateToken);

        if (mfaVerificationResponse.Status == AuthenticationStatus.Success)
        {
            return new AuthenticationResponse
            {
                Status = mfaVerificationResponse.Status,
                ExpiresAt = mfaVerificationResponse.ExpiresAt,
                SessionToken = mfaVerificationResponse.SessionToken,
                StateToken = mfaVerificationResponse.StateToken
            };
        }

        WriteErrorMessage("Failed!");
        WriteErrorMessage("MFA verification timeout or rejected. Please try again.");

        return null;
    }

    private static AuthenticationResult HandleFailedAuthenticationResponse(Uri oktaDomain, AuthenticationResponse authResponse)
    {
        var errorMessage = GetAuthErrorMessages(authResponse.Status) ?? authResponse.Status;

        WriteErrorMessage($"Authentication failed: {errorMessage}");

        return authResponse.Status switch
        {
            AuthenticationStatus.PasswordExpired => throw new PasswordExpiredException(),
            AuthenticationStatus.Unauthorized => throw new InvalidUsernameOrPasswordException(),
            _ => FailedResult(oktaDomain)
        };
    }

    private static void WriteErrorMessage(string message) => AnsiConsole.MarkupLine($"[bold red]{message}[/]");

    private static AuthenticationResult FailedResult(Uri oktaDomain, string? mfaUsed = null)
        => new() { OktaDomain = oktaDomain, MfaUsed = mfaUsed };

    private static AuthenticationResult SuccessfulResult(Uri oktaDomain, AuthenticationResponse authResponse, string? mfaUsed)
        => new()
        {
            OktaDomain = oktaDomain,
            MfaUsed = mfaUsed,
            StateToken = authResponse.StateToken,
            SessionToken = authResponse.SessionToken,
            Authenticated = true
        };

    private static string? GetAuthErrorMessages(string status) =>
         status switch
         {
             AuthenticationStatus.Unauthenticated => "You are not authenticated",
             AuthenticationStatus.PasswordExpired => "Password expired, please change your Okta password and try again",
             AuthenticationStatus.MfaEnroll => "MFA enrollment required, please setup your MFA in Okta and try again",
             AuthenticationStatus.LockedOut => "Okta account locked out",
             AuthenticationStatus.Unauthorized => "Invalid username or password. Please try again",
             _ => null
         };

    private static HttpClient CreateHttpClientWithCookieContainer()
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };

        var httpClient = new HttpClient(handler);
        httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

        return httpClient;
    }
}
