// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Collections.Concurrent;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;

namespace Ellosoft.AwsCredentialsManager.Tests.Integration.FakeApis;

/// <summary>
///     Fake Okta Identity Engine (IDX) API plus a fake Okta Verify loopback server (/probe, /challenge).
///     The sign-in transaction moves to "success" once the FastPass challenge has been delivered to the loopback server.
/// </summary>
[ApiController]
public class OktaIdxController : ControllerBase
{
    public const string StateToken = "02state-token";
    public const string StateHandle = "02state-handle";
    public const string ChallengeRequest = "eyJraWQ.fake.challenge.jwt";
    public const string SessionId = "102fastpass-session";

    private const string ION_JSON = "application/ion+json; okta-version=1.0.0";

    private static readonly ConcurrentDictionary<string, bool> ChallengeDelivered = new();
    private static readonly ConcurrentDictionary<string, bool> AppLaunchOffered = new();

    /// <summary>
    ///     Identity Engine orgs serve the End-User Dashboard SPA shell on the org root (no state token)
    /// </summary>
    [HttpGet("/")]
    public IActionResult DashboardShell() =>
        Content("""<html><head><meta name="ui-service" content="iris"/></head><body><div id="root"></div></body></html>""", "text/html");

    /// <summary>
    ///     The sign-in page (with the Identity Engine state token) is rendered by the OIDC authorize endpoint
    /// </summary>
    [HttpGet("/oauth2/v1/authorize")]
    public IActionResult SignInPage([FromQuery(Name = "client_id")] string clientId, [FromQuery(Name = "redirect_uri")] string redirectUri)
    {
        if (clientId != "okta.2b1959c8-bcc0-56eb-a589-cfcfb7422f26" || !redirectUri.EndsWith("/enduser/callback", StringComparison.Ordinal))
            return BadRequest("Unknown OIDC client");

        return Content($"<html><head><script>var stateToken = '{StateToken.Replace("-", @"\x2D")}';</script></head><body/></html>", "text/html");
    }

    [HttpPost("/idp/idx/introspect")]
    public IActionResult Introspect([FromBody] JsonElement request)
    {
        ChallengeDelivered[CorrelationId] = false;

        return Ion(
            $$"""
              {
                "version": "1.0.0",
                "stateHandle": "{{StateHandle}}",
                "intent": "LOGIN",
                "remediation": {
                  "type": "array",
                  "value": [
                    {
                      "rel": ["create-form"],
                      "name": "identify",
                      "href": "{{BaseUrl}}/idp/idx/identify",
                      "method": "POST",
                      "value": [
                        { "name": "identifier", "label": "Username", "required": true },
                        { "name": "credentials", "type": "object", "form": { "value": [ { "name": "passcode", "label": "Password", "secret": true } ] }, "required": true },
                        { "name": "stateHandle", "required": true, "value": "{{StateHandle}}", "visible": false, "mutable": false }
                      ]
                    }
                  ]
                }
              }
              """);
    }

    [HttpPost("/idp/idx/identify")]
    public IActionResult Identify([FromBody] JsonElement request) => Ion(ChallengePoll());

    [HttpPost("/idp/idx/authenticators/poll")]
    public IActionResult Poll([FromBody] JsonElement request)
    {
        if (ChallengeDelivered.TryGetValue(CorrelationId, out var delivered) && delivered)
        {
            return Ion(
                $$"""
                  {
                    "version": "1.0.0",
                    "stateHandle": "{{StateHandle}}",
                    "intent": "LOGIN",
                    "success": { "rel": ["create-form"], "name": "success-redirect", "href": "{{BaseUrl}}/login/token/redirect?stateToken={{StateHandle}}", "method": "GET" }
                  }
                  """);
        }

        return Ion(ChallengePoll());
    }

    [HttpPost("/idp/idx/authenticators/poll/cancel")]
    public IActionResult CancelPolling([FromBody] JsonElement request)
    {
        // like a real Identity Engine org: when Okta Verify is not reachable through loopback Okta offers to open the app
        if (request.GetProperty("reason").GetString() == "OV_UNREACHABLE_BY_LOOPBACK")
            return Ion(OffersAppLaunch(CorrelationId) ? LaunchAuthenticator() : ChallengePoll());

        return Ion(
            $$"""
              {
                "version": "1.0.0",
                "stateHandle": "{{StateHandle}}",
                "intent": "LOGIN",
                "messages": { "type": "array", "value": [ { "message": "Polling cancelled: {{request.GetProperty("reason").GetString()}}", "class": "ERROR" } ] }
              }
              """);
    }

    [HttpPost("/idp/idx/authenticators/okta-verify/launch")]
    public IActionResult LaunchOktaVerify([FromBody] JsonElement request) => Ion(DeviceChallengePollCustomUri());

    /// <summary>
    ///     Simulates the user approving the sign-in in the Okta Verify app (CUSTOM_URI challenge)
    /// </summary>
    public static void ApproveInApp(string correlationId) => ChallengeDelivered[correlationId] = true;

    /// <summary>
    ///     Controls whether the fake org offers the "launch-authenticator" remediation when loopback is cancelled (default: true)
    /// </summary>
    public static void SetOffersAppLaunch(string correlationId, bool offersAppLaunch) => AppLaunchOffered[correlationId] = offersAppLaunch;

    private static bool OffersAppLaunch(string correlationId) => !AppLaunchOffered.TryGetValue(correlationId, out var offered) || offered;

    [HttpGet("/login/token/redirect")]
    public IActionResult SuccessRedirect() => Content("<html><body>Okta Dashboard</body></html>", "text/html");

    // ---- fake Okta Verify loopback server ----

    [HttpGet("/probe")]
    public IActionResult Probe() => Ok();

    [HttpPost("/challenge")]
    public IActionResult Challenge([FromBody] JsonElement request)
    {
        if (request.GetProperty("challengeRequest").GetString() != ChallengeRequest)
            return BadRequest();

        ChallengeDelivered[CorrelationId] = true;

        return Ok();
    }

    private string CorrelationId => Request.Headers["Correlation-Id"].ToString();

    private string BaseUrl => $"{Request.Scheme}://{Request.Host}";

    private ContentResult Ion(string json) => Content(json, ION_JSON);

    private string ChallengePoll() =>
        $$"""
          {
            "version": "1.0.0",
            "stateHandle": "{{StateHandle}}",
            "intent": "LOGIN",
            "remediation": {
              "type": "array",
              "value": [
                {
                  "rel": ["create-form"],
                  "name": "challenge-poll",
                  "relatesTo": ["$.currentAuthenticator"],
                  "href": "{{BaseUrl}}/idp/idx/authenticators/poll",
                  "method": "POST",
                  "refresh": 500,
                  "value": [ { "name": "stateHandle", "required": true, "value": "{{StateHandle}}", "visible": false, "mutable": false } ]
                }
              ]
            },
            "currentAuthenticator": {
              "type": "object",
              "value": {
                "displayName": "Okta Verify",
                "type": "app",
                "key": "okta_verify",
                "id": "aut-okta-verify",
                "methods": [ { "type": "signed_nonce" } ],
                "cancel": { "rel": ["create-form"], "name": "cancel-polling", "href": "{{BaseUrl}}/idp/idx/authenticators/poll/cancel", "method": "POST" },
                "contextualData": {
                  "challenge": {
                    "type": "object",
                    "value": {
                      "challengeMethod": "LOOPBACK",
                      "challengeRequest": "{{ChallengeRequest}}",
                      "domain": "http://localhost",
                      "probeTimeoutMillis": 1000,
                      "ports": ["8769"]
                    }
                  }
                }
              }
            }
          }
          """;

    private string LaunchAuthenticator() =>
        $$"""
          {
            "version": "1.0.0",
            "stateHandle": "{{StateHandle}}",
            "intent": "LOGIN",
            "remediation": {
              "type": "array",
              "value": [
                {
                  "rel": ["create-form"],
                  "name": "launch-authenticator",
                  "relatesTo": ["$.authenticatorChallenge"],
                  "href": "{{BaseUrl}}/idp/idx/authenticators/okta-verify/launch",
                  "method": "POST",
                  "value": [ { "name": "stateHandle", "required": true, "value": "{{StateHandle}}", "visible": false, "mutable": false } ]
                },
                {
                  "rel": ["create-form"],
                  "name": "select-authenticator-authenticate",
                  "href": "{{BaseUrl}}/idp/idx/challenge",
                  "method": "POST",
                  "value": [ { "name": "stateHandle", "required": true, "value": "{{StateHandle}}", "visible": false, "mutable": false } ]
                }
              ]
            }
          }
          """;

    private string DeviceChallengePollCustomUri() =>
        $$"""
          {
            "version": "1.0.0",
            "stateHandle": "{{StateHandle}}",
            "intent": "LOGIN",
            "remediation": {
              "type": "array",
              "value": [
                {
                  "rel": ["create-form"],
                  "name": "device-challenge-poll",
                  "relatesTo": ["$.authenticatorChallenge"],
                  "href": "{{BaseUrl}}/idp/idx/authenticators/poll",
                  "method": "POST",
                  "refresh": 500,
                  "value": [ { "name": "stateHandle", "required": true, "value": "{{StateHandle}}", "visible": false, "mutable": false } ]
                }
              ]
            },
            "authenticatorChallenge": {
              "type": "object",
              "value": {
                "challengeMethod": "CUSTOM_URI",
                "href": "com-okta-authenticator:/deviceChallenge?challengeRequest={{ChallengeRequest}}",
                "cancel": { "rel": ["create-form"], "name": "cancel-polling", "href": "{{BaseUrl}}/idp/idx/authenticators/poll/cancel", "method": "POST" }
              }
            }
          }
          """;
}
