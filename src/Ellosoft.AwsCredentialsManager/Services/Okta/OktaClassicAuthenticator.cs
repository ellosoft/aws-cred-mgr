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

    public async Task<AuthenticationResult> Authenticate(Uri oktaDomain, string username, string password, string? preferredMfa)
    {
        try
        {
            AnsiConsole.MarkupLine("Authenticating...");

            var authResponse = await AuthenticateWithUsernameAndPassword(oktaDomain, username, password);

            if (authResponse.Status == AuthenticationStatus.MfaRequired)
            {
                AnsiConsole.MarkupLine("Verifying 2FA...");
                authResponse = await VerifyMfa(oktaDomain, authResponse, preferredMfa);

                if (authResponse is null)
                    return FailedResult(oktaDomain);
            }

            if (authResponse.Status == AuthenticationStatus.Success)
            {
                AnsiConsole.MarkupLine("[bold green]Authentication successful![/]");

                return SuccessfulResult(oktaDomain, authResponse);
            }

            HandleFailedAuthenticationResponse(authResponse);
        }
        catch (OktaApiException e) when (e.StatusCode == HttpStatusCode.Unauthorized)
        {
            WriteErrorMessage("Authentication failed: Invalid username or password. Please try again");

            throw new InvalidUsernameOrPasswordException(e);
        }
        catch (OktaApiException e)
        {
            WriteErrorMessage($"Authentication failed: {e.OktaApiError?.ErrorSummary}");
        }

        return FailedResult(oktaDomain);
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
            throw new OktaApiException(httpResponse.StatusCode, null);

        var apiError = await httpResponse.Content.ReadFromJsonAsync(OktaSourceGenerationContext.Default.OktaApiError);
        throw new OktaApiException(httpResponse.StatusCode, apiError);
    }

    private Task<AuthenticationResponse?> VerifyMfa(Uri oktaDomain, AuthenticationResponse authResponse, string? preferredMfaType)
    {
        if (authResponse.StateToken is null || authResponse.Embedded is null || authResponse.Embedded.Factors.Count == 0)
            throw new InvalidOperationException("Invalid Okta MFA authentication response");

        var factors = authResponse.Embedded.Factors;

        if (preferredMfaType is not null)
        {
            var preferredFactor = factors.FirstOrDefault(f => f.FactorType == preferredMfaType);

            if (preferredFactor is not null)
            {
                return ExecuteMfaFactorHandler(oktaDomain, preferredFactor, authResponse.StateToken);
            }
        }

        var selectedFactor = _mfaFactorSelector.GetMfaFactor(preferredMfaType, factors);

        return ExecuteMfaFactorHandler(oktaDomain, selectedFactor, authResponse.StateToken);
    }

    private async Task<AuthenticationResponse?> ExecuteMfaFactorHandler(Uri oktaDomain, OktaFactor factor, string stateToken)
    {
        try
        {
            var handler = _mfaHandlerProvider.GetOktaFactorHandler(_httpClient, factor.FactorType);

            var mfaResult = await handler.VerifyFactor(oktaDomain, factor, stateToken);
            mfaResult.SetProperty(nameof(AuthenticationResult.MfaUsed), factor.Type);

            return mfaResult;
        }
        catch (OktaApiException e) when (e.StatusCode == (int)HttpStatusCode.Unauthorized)
        {
            AnsiConsole.MarkupLine("[red]Failed![/]");
            AnsiConsole.MarkupLine("[red]MFA verification timeout. Please try again.[/]");

            return null;
        }
    }

    private static void HandleFailedAuthenticationResponse(AuthenticationResponse authResponse)
    {
        var errorMessage =
            GetAuthErrorMessages().TryGetValue(authResponse.AuthenticationStatus, out var errorDescription)
                ? errorDescription
                : authResponse.AuthenticationStatus.ToString();

        WriteErrorMessage($"Authentication failed: {errorMessage}");

        if (authResponse.AuthenticationStatus == AuthenticationStatus.PasswordExpired)
            throw new PasswordExpiredException();
    }

    private static void WriteErrorMessage(string message) => AnsiConsole.MarkupLine($"[bold red]{message}[/]");

    private static AuthenticationResult FailedResult(Uri oktaDomain, string? mfaUsed = null)
        => new() { OktaDomain = oktaDomain, MfaUsed = mfaUsed };

    private static AuthenticationResult SuccessfulResult(Uri oktaDomain, AuthenticationResponse authResponse)
        => new()
        {
            OktaDomain = oktaDomain,
            MfaUsed = authResponse.GetProperty<string>(nameof(AuthenticationResult.MfaUsed)),
            StateToken = authResponse.StateToken,
            SessionToken = authResponse.SessionToken,
            Authenticated = true
        };

    private static Dictionary<string, string> GetAuthErrorMessages() => new()
    {
        [AuthenticationStatus.Unauthenticated] = "You are not authenticated",
        [AuthenticationStatus.PasswordExpired] = "Password expired, please change your Okta password and try again",
        [AuthenticationStatus.MfaEnroll] = "MFA enrollment required, please setup your MFA in Okta and try again",
        [AuthenticationStatus.LockedOut] = "Okta account locked out"
    };

    private static HttpClient CreateHttpClientWithCookieContainer()
    {
        var handler = new HttpClientHandler
        {
            CookieContainer = new CookieContainer(),
            AllowAutoRedirect = false
        };

        return new HttpClient(handler);
    }
}
