// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;
using Microsoft.Extensions.Logging;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

public interface IOktaFastPassChallengeHandler
{
    /// <summary>
    ///     Delivers an Okta Verify (FastPass) device challenge to the Okta Verify app running on this machine and polls
    ///     Okta until the sign-in transaction moves on
    /// </summary>
    /// <param name="idxClient">IDX client bound to the current sign-in transaction</param>
    /// <param name="oktaDomain">Okta org domain</param>
    /// <param name="challengeResponse">IDX response containing a "challenge-poll" or "device-challenge-poll" remediation</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>
    ///     The IDX response that ended the polling: success, an error, or a new remediation step
    ///     (e.g. "launch-authenticator" after Okta Verify could not be reached through the loopback server)
    /// </returns>
    Task<IdxResponse> ExecuteAsync(IdxClient idxClient, Uri oktaDomain, IdxResponse challengeResponse, CancellationToken cancellationToken);
}

/// <summary>
///     Implements the two FastPass challenge bindings used by the Okta Sign-In Widget on desktop:
///     LOOPBACK (Okta Verify local HTTP server) and CUSTOM_URI (deep link that launches Okta Verify)
/// </summary>
public class OktaFastPassChallengeHandler(
    IOktaIdxHttpClientFactory httpClientFactory,
    IOktaVerifyAppLauncher appLauncher,
    IAnsiConsole console,
    ILogger<OktaFastPassChallengeHandler> logger) : IOktaFastPassChallengeHandler
{
    private const string REASON_UNREACHABLE = "OV_UNREACHABLE_BY_LOOPBACK";
    private const string REASON_ERROR = "OV_RETURNED_ERROR";
    private const string REASON_CANCELED = "USER_CANCELED";
    private const string DefaultLoopbackDomain = "http://localhost";

    private static readonly TimeSpan DefaultPollInterval = TimeSpan.FromSeconds(2);
    private static readonly TimeSpan MaxPollInterval = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan MinProbeTimeout = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Maximum time to wait for the user to approve the sign-in in Okta Verify
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     When Okta offers the loopback binding first, ask Okta to open Okta Verify (deep link) right away instead of
    ///     probing the loopback ports: the app comes to the foreground with the approval prompt, and it also works when
    ///     Okta Verify is not running. Loopback is still used when Okta does not offer to open the app.
    /// </summary>
    public bool PreferAppLaunch { get; set; } = true;

    public async Task<IdxResponse> ExecuteAsync(IdxClient idxClient, Uri oktaDomain, IdxResponse challengeResponse, CancellationToken cancellationToken)
    {
        var context = PollingContext.Create(challengeResponse, oktaDomain);

        using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutCts.CancelAfter(Timeout);

        try
        {
            if (PreferAppLaunch && context.Challenge.IsLoopback)
            {
                var appLaunchResponse = await RequestAppLaunchAsync(idxClient, context, timeoutCts.Token);

                if (!KeepsLoopbackChallenge(appLaunchResponse))
                    return appLaunchResponse;

                logger.LogDebug("Okta did not offer to open Okta Verify, falling back to the loopback server");

                context = PollingContext.Create(appLaunchResponse, oktaDomain);
            }

            return await PollAsync(idxClient, oktaDomain, context, timeoutCts.Token);
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            await CancelPollingAsync(idxClient, context, REASON_CANCELED, null, CancellationToken.None);

            throw new OktaFastPassException("Timed out waiting for Okta Verify. Please try again");
        }
    }

    /// <summary>
    ///     Cancels the loopback challenge the same way the Sign-In Widget does when Okta Verify is not reachable;
    ///     Okta then returns the "launch-authenticator" remediation carrying the Okta Verify deep link
    /// </summary>
    private Task<IdxResponse> RequestAppLaunchAsync(IdxClient idxClient, PollingContext context, CancellationToken cancellationToken)
    {
        console.MarkupLine("Requesting Okta to open Okta Verify on this device...");

        return CancelPollingAsync(idxClient, context, REASON_UNREACHABLE, null, cancellationToken);
    }

    private static bool KeepsLoopbackChallenge(IdxResponse response) =>
        response is { IsSuccess: false, HasErrors: false, PollRemediation: not null, DeviceChallenge.IsLoopback: true }
        && response.GetRemediation(IdxResponse.LaunchAuthenticatorRemediation) is null;

    private async Task<IdxResponse> PollAsync(IdxClient idxClient, Uri oktaDomain, PollingContext context, CancellationToken cancellationToken)
    {
        var loopbackTask = await DeliverChallengeAsync(idxClient, oktaDomain, context, cancellationToken);

        console.MarkupLine("Waiting for Okta Verify...");

        while (true)
        {
            if (loopbackTask is { IsCompleted: true })
            {
                var outcome = await loopbackTask;
                loopbackTask = null;

                var cancelResponse = await HandleLoopbackOutcomeAsync(idxClient, context, outcome, cancellationToken);

                if (cancelResponse is not null)
                    return cancelResponse;
            }
            else
            {
                await Task.Delay(GetPollInterval(context.PollRemediation), cancellationToken);
            }

            var response = await idxClient.PostAsync(context.PollRemediation.Href, context.StateHandle, cancellationToken);

            if (response.IsSuccess || response.HasErrors || response.DeviceChallenge is null || response.PollRemediation is null)
                return response;

            var previousChallenge = context.Challenge;
            context = context.Update(response);

            // Okta issued a new challenge (e.g. user verification step-up), deliver it as well
            if (!IsSameChallenge(previousChallenge, context.Challenge))
                loopbackTask = await DeliverChallengeAsync(idxClient, oktaDomain, context, cancellationToken);
        }
    }

    /// <summary>
    ///     Starts the loopback challenge (returned task completes when Okta Verify accepts or refuses the challenge)
    ///     or launches Okta Verify through its deep link
    /// </summary>
    private async Task<Task<LoopbackOutcome>?> DeliverChallengeAsync(IdxClient idxClient, Uri oktaDomain, PollingContext context, CancellationToken cancellationToken)
    {
        var challenge = context.Challenge;

        if (challenge.IsLoopback)
        {
            console.MarkupLine("Contacting Okta Verify on this device...");

            return RunLoopbackAsync(challenge, oktaDomain, cancellationToken);
        }

        console.MarkupLine("Opening Okta Verify... Please approve the sign-in request in the app");

        if (challenge.Href is not null && appLauncher.TryLaunch(challenge.Href))
            return null;

        logger.LogError("Unable to open Okta Verify using challenge method {Method}", challenge.Method);

        await CancelPollingAsync(idxClient, context, REASON_CANCELED, null, CancellationToken.None);

        throw new OktaFastPassException("Unable to open Okta Verify. Please make sure Okta Verify is installed on this device and try again");
    }

    private async Task<IdxResponse?> HandleLoopbackOutcomeAsync(IdxClient idxClient, PollingContext context, LoopbackOutcome outcome, CancellationToken cancellationToken)
    {
        switch (outcome.Kind)
        {
            case LoopbackOutcomeKind.Unreachable:
                console.MarkupLine("[yellow]Okta Verify could not be reached on this device, trying to open the app instead...[/]");

                return await CancelPollingAsync(idxClient, context, REASON_UNREACHABLE, null, cancellationToken);

            case LoopbackOutcomeKind.Error:
                return await CancelPollingAsync(idxClient, context, REASON_ERROR, outcome.StatusCode, cancellationToken);

            default:
                return null;
        }
    }

    /// <summary>
    ///     Mirrors the Sign-In Widget loopback probe: GET {domain}:{port}/probe for each port, then
    ///     POST {domain}:{port}/challenge with the challenge JWT on the first port that answers
    /// </summary>
    private async Task<LoopbackOutcome> RunLoopbackAsync(IdxDeviceChallenge challenge, Uri oktaDomain, CancellationToken cancellationToken)
    {
        var domain = challenge.Domain ?? DefaultLoopbackDomain;

        // the domain comes from the Okta response: the challenge JWT is only ever sent to the loopback interface over plain http
        if (!Uri.TryCreate(domain, UriKind.Absolute, out var domainUri) || domainUri.Scheme != Uri.UriSchemeHttp || !domainUri.IsLoopback)
        {
            logger.LogError("Ignoring Okta Verify loopback challenge for non-loopback domain {Domain}", domain);

            return LoopbackOutcome.Unreachable;
        }

        using var loopbackClient = httpClientFactory.CreateLoopbackClient();

        var probeTimeout = TimeSpan.FromMilliseconds(Math.Max(challenge.ProbeTimeoutMillis ?? 0, MinProbeTimeout.TotalMilliseconds));
        var origin = oktaDomain.GetLeftPart(UriPartial.Authority);
        var challengeBody = new JsonObject { ["challengeRequest"] = challenge.ChallengeRequest }.ToJsonString();

        foreach (var port in challenge.Ports)
        {
            var baseUrl = new UriBuilder(domainUri) { Port = port, Path = string.Empty, Query = string.Empty }.Uri.GetLeftPart(UriPartial.Authority);

            try
            {
                if (!await ProbeAsync(loopbackClient, baseUrl, probeTimeout, cancellationToken))
                    continue;

                using var request = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/challenge");
                request.Headers.TryAddWithoutValidation("Origin", origin);
                request.Content = new StringContent(challengeBody, Encoding.UTF8, "application/json");

                using var response = await loopbackClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                    return LoopbackOutcome.Delivered;

                // Okta Verify answers 503 when another OS user profile owns the port
                if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
                    continue;

                logger.LogError("Okta Verify loopback server at {BaseUrl} rejected the challenge with status {StatusCode}", baseUrl, (int)response.StatusCode);

                return LoopbackOutcome.Error((int)response.StatusCode);
            }
            catch (Exception e) when (e is HttpRequestException or OperationCanceledException or IOException)
            {
                logger.LogDebug(e, "Okta Verify is not listening on {BaseUrl}", baseUrl);
            }
        }

        return LoopbackOutcome.Unreachable;
    }

    private static async Task<bool> ProbeAsync(HttpClient loopbackClient, string baseUrl, TimeSpan probeTimeout, CancellationToken cancellationToken)
    {
        using var probeCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        probeCts.CancelAfter(probeTimeout);

        using var probeResponse = await loopbackClient.GetAsync($"{baseUrl}/probe", probeCts.Token);

        return probeResponse.IsSuccessStatusCode;
    }

    private static async Task<IdxResponse> CancelPollingAsync(IdxClient idxClient, PollingContext context, string reason, int? statusCode,
        CancellationToken cancellationToken)
    {
        var body = new JsonObject
        {
            ["stateHandle"] = context.StateHandle,
            ["reason"] = reason,
            ["statusCode"] = statusCode
        };

        return await idxClient.PostAsync(context.CancelHref, body, cancellationToken);
    }

    private static bool IsSameChallenge(IdxDeviceChallenge current, IdxDeviceChallenge next) =>
        current.Method == next.Method && current.ChallengeRequest == next.ChallengeRequest && current.Href == next.Href;

    private static TimeSpan GetPollInterval(IdxRemediation pollRemediation)
    {
        var interval = pollRemediation.Refresh is { } refresh ? TimeSpan.FromMilliseconds(refresh) : DefaultPollInterval;

        return interval > MaxPollInterval ? MaxPollInterval : interval;
    }

    private sealed record PollingContext(IdxDeviceChallenge Challenge, IdxRemediation PollRemediation, string StateHandle, string CancelHref)
    {
        public static PollingContext Create(IdxResponse response, Uri oktaDomain) => new(
            response.DeviceChallenge ?? throw new InvalidOperationException("Okta response does not contain a device challenge"),
            response.PollRemediation ?? throw new InvalidOperationException("Okta response does not contain a poll remediation"),
            response.StateHandle ?? throw new InvalidOperationException("Okta response does not contain a state handle"),
            response.CancelPollingHref ?? new Uri(oktaDomain, "/idp/idx/authenticators/poll/cancel").ToString());

        public PollingContext Update(IdxResponse response) => this with
        {
            Challenge = response.DeviceChallenge ?? Challenge,
            PollRemediation = response.PollRemediation ?? PollRemediation,
            StateHandle = response.StateHandle ?? StateHandle,
            CancelHref = response.CancelPollingHref ?? CancelHref
        };
    }

    private enum LoopbackOutcomeKind
    {
        Delivered,
        Unreachable,
        Error
    }

    private sealed record LoopbackOutcome(LoopbackOutcomeKind Kind, int? StatusCode = null)
    {
        public static readonly LoopbackOutcome Delivered = new(LoopbackOutcomeKind.Delivered);
        public static readonly LoopbackOutcome Unreachable = new(LoopbackOutcomeKind.Unreachable);

        public static LoopbackOutcome Error(int statusCode) => new(LoopbackOutcomeKind.Error, statusCode);
    }
}
