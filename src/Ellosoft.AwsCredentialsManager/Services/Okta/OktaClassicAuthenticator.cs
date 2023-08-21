// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using AngleSharp.Html.Parser;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Okta.Auth.Sdk;
using Okta.Sdk.Abstractions;
using Okta.Sdk.Abstractions.Configuration;

namespace Ellosoft.AwsCredentialsManager.Services.Okta;

public class OktaClassicAuthenticator
{
    private readonly Func<string, IAuthenticationClient> _authClientProvider;
    private readonly MfaHandlerProvider _mfaHandlerProvider = new();
    private readonly OktaMfaFactorSelector _mfaFactorSelector = new();

    public OktaClassicAuthenticator()
    {
        _authClientProvider = oktaDomain => new AuthenticationClient(new OktaClientConfiguration
        {
            OktaDomain = oktaDomain
        });
    }

    public async Task<string?> Authenticate(string oktaDomain, string username, string password, string? preferredMfa)
    {
        var authOptions = new AuthenticateOptions
        {
            Username = username,
            Password = password
        };

        try
        {
            AnsiConsole.MarkupLine("Authenticating...");

            var authClient = _authClientProvider(oktaDomain);
            var authResponse = await authClient.AuthenticateAsync(authOptions);

            if (authResponse.AuthenticationStatus == AuthenticationStatus.MfaRequired)
            {
                AnsiConsole.MarkupLine("Verifying 2FA...");
                authResponse = await VerifyMfa(authClient, authResponse, preferredMfa);

                if (authResponse is null)
                    return null;
            }

            if (authResponse.AuthenticationStatus == AuthenticationStatus.Success)
            {
                AnsiConsole.MarkupLine("[bold green]Authentication successful![/]");

                return authResponse.SessionToken;
            }

            HandleFailedAuthenticationResponse(authResponse);
        }
        catch (OktaApiException e) when (e.StatusCode == 401)
        {
            WriteErrorMessage("Authentication failed: Invalid username or password. Please try again");

            throw new InvalidUsernameOrPasswordException(e);
        }
        catch (OktaApiException e)
        {
            WriteErrorMessage($"Authentication failed: {e.ErrorSummary}");
        }

        return null;
    }

    public async Task<SamlData> GetAppSamlData(string oktaDomain, string sessionToken, string appLink)
    {
        using var response = await RedirectUsingSessionCookie(oktaDomain, sessionToken, appLink);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"Failed to retrieve SAML assertion. HTTP Status: {response.StatusCode}");

        var responseBody = await response.Content.ReadAsStringAsync();

        var parser = new HtmlParser();
        using var document = parser.ParseDocument(responseBody);

        return new SamlData
        (
            SamlAssertion: document.QuerySelector("input[name=SAMLResponse]")?.GetAttribute("value")!,
            SignInUrl: document.QuerySelector("form")?.GetAttribute("action")!,
            RelayState: document.QuerySelector("input[name=RelayState]")?.GetAttribute("value") ?? String.Empty
        );
    }

    private Task<IAuthenticationResponse?> VerifyMfa(IAuthenticationClient authClient, IAuthenticationResponse authResponse, string? preferredMfaType)
    {
        var factors = authResponse.Embedded.GetArrayProperty<Factor>("factors");

        if (preferredMfaType is not null)
        {
            var preferredFactor = factors.FirstOrDefault(f => f.Type == preferredMfaType);

            if (preferredFactor is not null)
            {
                return ExecuteMfaFactorHandler(authClient, preferredFactor, authResponse.StateToken);
            }
        }

        var selectedFactor = _mfaFactorSelector.GetMfaFactor(preferredMfaType, factors);

        return ExecuteMfaFactorHandler(authClient, selectedFactor, authResponse.StateToken);
    }

    private async Task<IAuthenticationResponse?> ExecuteMfaFactorHandler(IAuthenticationClient authClient, Factor factor, string stateToken)
    {
        try
        {
            var handler = _mfaHandlerProvider.GetOktaFactorHandler(factor.Type, authClient);

            return await handler.VerifyFactor(factor.Id, stateToken);
        }
        catch (OktaApiException e) when (e.StatusCode == 401)
        {
            AnsiConsole.MarkupLine("[red]Failed![/]");
            AnsiConsole.MarkupLine("[red]MFA verification timeout. Please try again.[/]");

            return null;
        }
    }

    private static async Task<HttpResponseMessage> RedirectUsingSessionCookie(string oktaDomain, string sessionToken, string redirectUrl)
    {
        // see: https://developer.okta.com/docs/guides/session-cookie/main/#retrieve-a-session-cookie-by-visiting-a-session-redirect-link
        const string OKTA_SESSION_REDIRECT_URL_TEMPLATE = "/login/sessionCookieRedirect?token=${sessionToken}&redirectUrl=${redirectUrl}";

        var sessionRedirectUrl = OKTA_SESSION_REDIRECT_URL_TEMPLATE
            .Replace("${sessionToken}", sessionToken)
            .Replace("${redirectUrl}", redirectUrl);

        using var httpClient = new HttpClient();

        return await httpClient.GetAsync(new Uri(oktaDomain.TrimEnd('/') + sessionRedirectUrl));
    }

    private static void HandleFailedAuthenticationResponse(IAuthenticationResponse authResponse)
    {
        var errorMessage =
            GetAuthErrorMessages().TryGetValue(authResponse.AuthenticationStatus, out var errorDescription)
                ? errorDescription
                : authResponse.AuthenticationStatus.ToString();

        WriteErrorMessage($"Authentication failed: {errorMessage}");

        if (authResponse.AuthenticationStatus == AuthenticationStatus.PasswordExpired)
            throw new PasswordExpiredException();
    }

    private static Dictionary<AuthenticationStatus, string> GetAuthErrorMessages() => new()
    {
        [AuthenticationStatus.Unauthenticated] = "You are not authenticated",
        [AuthenticationStatus.PasswordExpired] = "Password expired, please change your Okta password and try again",
        [AuthenticationStatus.MfaEnroll] = "MFA enrollment required, please setup your MFA in Okta and try again",
        [AuthenticationStatus.LockedOut] = "Okta account locked out"
    };

    private static void WriteErrorMessage(string message) => AnsiConsole.MarkupLine($"[bold red]{message}[/]");
}
