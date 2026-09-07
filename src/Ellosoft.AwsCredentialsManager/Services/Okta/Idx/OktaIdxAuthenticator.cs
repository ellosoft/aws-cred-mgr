// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Buffers.Text;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json.Nodes;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models;
using Microsoft.Extensions.Logging;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

public interface IOktaIdxAuthenticator
{
    /// <summary>
    ///     Authenticates the user with Okta Identity Engine using password + Okta FastPass (Okta Verify desktop app)
    /// </summary>
    /// <returns>
    ///     Authentication result carrying the Okta session id (there is no session token in the Identity Engine flow)
    /// </returns>
    Task<AuthenticationResult> AuthenticateAsync(Uri oktaDomain, string username, string password, CancellationToken cancellationToken = default);
}

/// <summary>
///     Okta Identity Engine (IDX) remediation state machine driving a password + FastPass sign-in
/// </summary>
public class OktaIdxAuthenticator(
    IOktaIdxHttpClientFactory httpClientFactory,
    IOktaFastPassChallengeHandler challengeHandler,
    IAnsiConsole console,
    ILogger<OktaIdxAuthenticator> logger) : IOktaIdxAuthenticator
{
    /// <summary>
    ///     OIDC client id of the built-in Okta End-User Dashboard application (same in every Okta org)
    /// </summary>
    public const string OktaDashboardClientId = "okta.2b1959c8-bcc0-56eb-a589-cfcfb7422f26";

    private const string OKTA_VERIFY_AUTHENTICATOR_KEY = "okta_verify";
    private const string OKTA_PASSWORD_AUTHENTICATOR_KEY = "okta_password";
    private const string INVALID_CREDENTIALS_ERROR_KEY = "errors.E0000004";
    private const int MAX_REMEDIATION_STEPS = 15;

    public async Task<AuthenticationResult> AuthenticateAsync(Uri oktaDomain, string username, string password, CancellationToken cancellationToken = default)
    {
        console.MarkupLine("Authenticating...");

        // the cookies Okta sets during the sign-in (sid, idx, device token...) are the resulting session, keep them for later requests
        var sessionCookies = new CookieContainer();

        using var idxClient = new IdxClient(httpClientFactory.CreateSessionClient(sessionCookies));

        var stateToken = await GetLoginPageStateTokenAsync(idxClient, oktaDomain, cancellationToken);

        var response = await idxClient.PostAsync(new Uri(oktaDomain, "/idp/idx/introspect").ToString(), new JsonObject { ["stateToken"] = stateToken }, cancellationToken);

        var transaction = new Transaction(username, password);

        for (var step = 0; step < MAX_REMEDIATION_STEPS; step++)
        {
            if (response.IsSuccess)
            {
                var sessionId = await CompleteLoginAsync(idxClient, oktaDomain, response, cancellationToken);

                console.MarkupLine("\r\n[bold green]Authenticated![/]\r\n");

                return new AuthenticationResult
                {
                    OktaDomain = oktaDomain,
                    Authenticated = true,
                    SessionId = sessionId,
                    SessionCookies = sessionCookies,
                    MfaUsed = OktaMfaFactorSelector.FastPassFactorCode
                };
            }

            if (response.HasErrors)
                return HandleErrors(oktaDomain, response);

            response = await ExecuteNextRemediationAsync(idxClient, oktaDomain, response, transaction, cancellationToken);
        }

        throw new OktaFastPassException("Okta sign-in did not complete after too many steps. Please try again");
    }

    private async Task<IdxResponse> ExecuteNextRemediationAsync(IdxClient idxClient, Uri oktaDomain, IdxResponse response, Transaction transaction,
        CancellationToken cancellationToken)
    {
        var stateHandle = response.StateHandle ?? throw new OktaFastPassException("Okta response does not contain a state handle");

        if (response.PollRemediation is not null)
        {
            console.MarkupLine("Executing multi-factor authentication with [green]Okta FastPass[/]...");

            return await challengeHandler.ExecuteAsync(idxClient, oktaDomain, response, cancellationToken);
        }

        if (response.GetRemediation(IdxResponse.ChallengeAuthenticatorRemediation) is { } challengeAuthenticator)
            return await AnswerPasswordChallengeAsync(idxClient, response, challengeAuthenticator, stateHandle, transaction, cancellationToken);

        if (!transaction.Identified && response.GetRemediation(IdxResponse.IdentifyRemediation) is { } identify)
            return await IdentifyAsync(idxClient, response, identify, stateHandle, transaction, cancellationToken);

        // "launch-authenticator" (open Okta Verify) is offered next to "select-authenticator-authenticate"; take it as soon as the
        // password step is done, otherwise selecting Okta Verify again would just produce another loopback challenge
        if (response.GetRemediation(IdxResponse.LaunchAuthenticatorRemediation) is { } launchAuthenticator
            && (transaction.PasswordProvided || response.GetAuthenticatorOption(OKTA_PASSWORD_AUTHENTICATOR_KEY) is null))
        {
            return await idxClient.PostAsync(launchAuthenticator.Href, stateHandle, cancellationToken);
        }

        if (response.GetRemediation(IdxResponse.SelectAuthenticatorRemediation) is { } selectAuthenticator)
            return await SelectAuthenticatorAsync(idxClient, response, selectAuthenticator, stateHandle, transaction, cancellationToken);

        logger.LogError("Unsupported Okta Identity Engine remediation: {Remediations}", string.Join(", ", response.RemediationNames));

        // the full response carries the live state handle, only write it to the log file when debug logging is requested
        logger.LogDebug("Unsupported Okta Identity Engine response: {Response}", response);

        throw new OktaFastPassException(
            $"Okta requested a sign-in step that is not supported by this tool: {string.Join(", ", response.RemediationNames)}");
    }

    private static Task<IdxResponse> IdentifyAsync(IdxClient idxClient, IdxResponse response, IdxRemediation identify, string stateHandle,
        Transaction transaction, CancellationToken cancellationToken)
    {
        transaction.Identified = true;

        var body = new JsonObject
        {
            ["identifier"] = transaction.Username,
            ["stateHandle"] = stateHandle
        };

        if (response.IdentifyRequiresPassword)
        {
            body["credentials"] = new JsonObject { ["passcode"] = transaction.Password };
            transaction.PasswordProvided = true;
        }

        return idxClient.PostAsync(identify.Href, body, cancellationToken);
    }

    private static Task<IdxResponse> AnswerPasswordChallengeAsync(IdxClient idxClient, IdxResponse response, IdxRemediation challengeAuthenticator,
        string stateHandle, Transaction transaction, CancellationToken cancellationToken)
    {
        if (response.CurrentAuthenticatorKey is not (null or OKTA_PASSWORD_AUTHENTICATOR_KEY))
        {
            throw new OktaFastPassException(
                $"Okta requested verification with '{response.CurrentAuthenticatorKey}', which is not supported by the FastPass flow");
        }

        transaction.PasswordProvided = true;

        var body = new JsonObject
        {
            ["credentials"] = new JsonObject { ["passcode"] = transaction.Password },
            ["stateHandle"] = stateHandle
        };

        return idxClient.PostAsync(challengeAuthenticator.Href, body, cancellationToken);
    }

    private static Task<IdxResponse> SelectAuthenticatorAsync(IdxClient idxClient, IdxResponse response, IdxRemediation selectAuthenticator,
        string stateHandle, Transaction transaction, CancellationToken cancellationToken)
    {
        if (!transaction.PasswordProvided && response.GetAuthenticatorOption(OKTA_PASSWORD_AUTHENTICATOR_KEY) is { } passwordOption)
            return PostAuthenticatorSelection(idxClient, selectAuthenticator, stateHandle, passwordOption.Id, null, cancellationToken);

        var oktaVerifyOption = response.GetAuthenticatorOption(OKTA_VERIFY_AUTHENTICATOR_KEY);

        if (oktaVerifyOption is null || !oktaVerifyOption.MethodTypes.Contains(OktaMfaFactorSelector.FastPassFactorCode))
        {
            throw new OktaFastPassException(
                "Okta FastPass is not available for this account. " +
                $"Authenticators offered by Okta: {string.Join(", ", response.AuthenticatorOptionLabels)}. " +
                "Please enroll this device in Okta Verify or configure a different MFA type (push, totp)");
        }

        return PostAuthenticatorSelection(idxClient, selectAuthenticator, stateHandle, oktaVerifyOption.Id, OktaMfaFactorSelector.FastPassFactorCode, cancellationToken);
    }

    private static Task<IdxResponse> PostAuthenticatorSelection(IdxClient idxClient, IdxRemediation selectAuthenticator, string stateHandle,
        string authenticatorId, string? methodType, CancellationToken cancellationToken)
    {
        var authenticator = new JsonObject { ["id"] = authenticatorId };

        if (methodType is not null)
            authenticator["methodType"] = methodType;

        var body = new JsonObject
        {
            ["authenticator"] = authenticator,
            ["stateHandle"] = stateHandle
        };

        return idxClient.PostAsync(selectAuthenticator.Href, body, cancellationToken);
    }

    private AuthenticationResult HandleErrors(Uri oktaDomain, IdxResponse response)
    {
        if (response.ErrorMessages.Any(m => m.Key == INVALID_CREDENTIALS_ERROR_KEY))
        {
            console.MarkupLine("[bold red]Authentication failed: Invalid username or password. Please try again[/]");

            throw new InvalidUsernameOrPasswordException();
        }

        foreach (var message in response.ErrorMessages)
            console.MarkupLine($"[bold red]Authentication failed: {Markup.Escape(message.Message)}[/]");

        return new AuthenticationResult { OktaDomain = oktaDomain, Authenticated = false };
    }

    /// <summary>
    ///     Loads the Okta hosted sign-in page and extracts the Identity Engine state token that bootstraps the IDX transaction.
    ///     On Identity Engine orgs the org root serves the End-User Dashboard SPA shell (no state token); the sign-in page is
    ///     rendered by the OIDC authorize endpoint, which is the same request the dashboard issues in a browser. The org root
    ///     is kept as a fallback for orgs whose hosted sign-in page still embeds the token directly.
    /// </summary>
    private async Task<string> GetLoginPageStateTokenAsync(IdxClient idxClient, Uri oktaDomain, CancellationToken cancellationToken)
    {
        string[] signInPageUrls = [BuildDashboardAuthorizeUrl(oktaDomain), oktaDomain.ToString()];

        foreach (var signInPageUrl in signInPageUrls)
        {
            using var response = await idxClient.HttpClient.GetAsync(signInPageUrl, cancellationToken);

            var html = await response.Content.ReadAsStringAsync(cancellationToken);

            if (OktaLoginPageStateTokenExtractor.Extract(html) is { } stateToken)
                return stateToken;

            logger.LogDebug("No Identity Engine state token found in {SignInPageUrl} (HTTP {StatusCode}, final URL: {FinalUrl})",
                signInPageUrl, (int)response.StatusCode, response.RequestMessage?.RequestUri);
        }

        throw new OktaFastPassException(
            "Okta FastPass requires Okta Identity Engine, but the Okta sign-in page did not return an Identity Engine state token. " +
            "Please check that the Okta domain is correct and your org uses Okta Identity Engine, or use a different MFA type (push, totp)");
    }

    private static string BuildDashboardAuthorizeUrl(Uri oktaDomain)
    {
        // PKCE values are never redeemed (we only need the sign-in page), but they must be well-formed for Okta to render it
        var codeChallenge = Base64Url.EncodeToString(SHA256.HashData(RandomNumberGenerator.GetBytes(32)));

        var query = new Dictionary<string, string>
        {
            ["client_id"] = OktaDashboardClientId,
            ["redirect_uri"] = new Uri(oktaDomain, "/enduser/callback").ToString(),
            ["response_type"] = "code",
            ["scope"] = "openid profile email okta.users.read.self",
            ["state"] = RandomNumberGenerator.GetHexString(32, lowercase: true),
            ["nonce"] = RandomNumberGenerator.GetHexString(32, lowercase: true),
            ["code_challenge"] = codeChallenge,
            ["code_challenge_method"] = "S256"
        };

        var queryString = string.Join('&', query.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));

        return new Uri(oktaDomain, $"/oauth2/v1/authorize?{queryString}").ToString();
    }

    /// <summary>
    ///     Follows the IDX success redirect (this sets the Okta session cookie) and resolves the session id
    /// </summary>
    private static async Task<string> CompleteLoginAsync(IdxClient idxClient, Uri oktaDomain, IdxResponse response, CancellationToken cancellationToken)
    {
        if (response.SuccessHref is { } successHref)
        {
            using var redirectResponse = await idxClient.HttpClient.GetAsync(successHref, cancellationToken);
        }

        using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(oktaDomain, "/api/v1/sessions/me"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var sessionResponse = await idxClient.HttpClient.SendAsync(request, cancellationToken);

        if (!sessionResponse.IsSuccessStatusCode)
            throw new OktaFastPassException($"Okta sign-in succeeded but the Okta session could not be retrieved ({(int)sessionResponse.StatusCode})");

        var session = JsonNode.Parse(await sessionResponse.Content.ReadAsStringAsync(cancellationToken));

        return session?["id"]?.GetValue<string>() ?? throw new OktaFastPassException("Okta session response does not contain a session id");
    }

    private sealed class Transaction(string username, string password)
    {
        public string Username { get; } = username;

        public string Password { get; } = password;

        public bool Identified { get; set; }

        public bool PasswordProvided { get; set; }
    }
}
