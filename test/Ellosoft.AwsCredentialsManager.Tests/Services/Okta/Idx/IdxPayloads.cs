// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Tests.Services.Okta.Idx;

/// <summary>
///     IDX (Okta Identity Engine) payloads modelled on the Okta Sign-In Widget mocks
///     (playground/mocks/data/idp/idx in okta/okta-signin-widget)
/// </summary>
public static class IdxPayloads
{
    public const string StateHandle = "02state-handle";

    public const string IdentifyWithPassword =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "expiresAt": "2026-09-04T18:00:00.000Z",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "identify",
                "href": "https://xyz.okta.com/idp/idx/identify",
                "method": "POST",
                "produces": "application/ion+json; okta-version=1.0.0",
                "value": [
                  { "name": "identifier", "label": "Username", "required": true },
                  {
                    "name": "credentials",
                    "type": "object",
                    "form": { "value": [ { "name": "passcode", "label": "Password", "secret": true } ] },
                    "required": true
                  },
                  { "name": "rememberMe", "type": "boolean", "label": "Remember this device" },
                  { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false }
                ],
                "accepts": "application/json; okta-version=1.0.0"
              },
              {
                "rel": ["create-form"],
                "name": "select-enroll-profile",
                "href": "https://xyz.okta.com/idp/idx/enroll",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              }
            ]
          },
          "cancel": { "rel": ["create-form"], "name": "cancel", "href": "https://xyz.okta.com/idp/idx/cancel", "method": "POST" },
          "app": { "type": "object", "value": { "name": "okta_enduser", "label": "Okta Dashboard", "id": "DEFAULT_APP" } }
        }
        """;

    public const string IdentifyWithoutPassword =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "identify",
                "href": "https://xyz.okta.com/idp/idx/identify",
                "method": "POST",
                "value": [
                  { "name": "identifier", "label": "Username", "required": true },
                  { "name": "rememberMe", "type": "boolean", "label": "Remember this device" },
                  { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false }
                ]
              },
              {
                "rel": ["create-form"],
                "name": "launch-authenticator",
                "href": "https://xyz.okta.com/idp/idx/authenticators/okta-verify/launch",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              }
            ]
          }
        }
        """;

    public const string SelectAuthenticator =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "select-authenticator-authenticate",
                "href": "https://xyz.okta.com/idp/idx/challenge",
                "method": "POST",
                "value": [
                  {
                    "name": "authenticator",
                    "type": "object",
                    "options": [
                      {
                        "label": "Okta Verify",
                        "value": {
                          "form": {
                            "value": [
                              { "name": "id", "value": "aut-okta-verify", "required": true, "mutable": false },
                              {
                                "name": "methodType",
                                "type": "string",
                                "required": false,
                                "options": [
                                  { "value": "signed_nonce", "label": "Use Okta FastPass" },
                                  { "value": "push", "label": "Get a push notification" },
                                  { "value": "totp", "label": "Enter a code" }
                                ]
                              }
                            ]
                          }
                        },
                        "relatesTo": "$.authenticatorEnrollments.value[0]"
                      },
                      {
                        "label": "Password",
                        "value": {
                          "form": {
                            "value": [
                              { "name": "id", "value": "aut-password", "required": true, "mutable": false },
                              { "name": "methodType", "value": "password", "required": false, "mutable": false }
                            ]
                          }
                        },
                        "relatesTo": "$.authenticatorEnrollments.value[1]"
                      }
                    ]
                  },
                  { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false }
                ]
              }
            ]
          },
          "authenticatorEnrollments": {
            "type": "array",
            "value": [
              { "type": "app", "key": "okta_verify", "id": "enr-okta-verify", "displayName": "Okta Verify", "methods": [ { "type": "signed_nonce" }, { "type": "push" }, { "type": "totp" } ] },
              { "type": "password", "key": "okta_password", "id": "enr-password", "displayName": "Password", "methods": [ { "type": "password" } ] }
            ]
          },
          "user": { "type": "object", "value": { "id": "00u1" } }
        }
        """;

    public const string ChallengePasswordAuthenticator =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "challenge-authenticator",
                "relatesTo": ["$.currentAuthenticatorEnrollment"],
                "href": "https://xyz.okta.com/idp/idx/challenge/answer",
                "method": "POST",
                "value": [
                  { "name": "credentials", "type": "object", "form": { "value": [ { "name": "passcode", "label": "Password", "secret": true } ] }, "required": true },
                  { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false }
                ]
              },
              {
                "rel": ["create-form"],
                "name": "select-authenticator-authenticate",
                "href": "https://xyz.okta.com/idp/idx/challenge",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              }
            ]
          },
          "currentAuthenticatorEnrollment": {
            "type": "object",
            "value": { "type": "password", "key": "okta_password", "id": "enr-password", "displayName": "Password", "methods": [ { "type": "password" } ] }
          },
          "currentAuthenticator": {
            "type": "object",
            "value": { "type": "password", "key": "okta_password", "id": "aut-password", "displayName": "Password", "methods": [ { "type": "password" } ] }
          }
        }
        """;

    public const string ChallengePollLoopback =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "challenge-poll",
                "relatesTo": ["$.currentAuthenticator"],
                "href": "https://xyz.okta.com/idp/idx/authenticators/poll",
                "method": "POST",
                "refresh": 4000,
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              },
              {
                "rel": ["create-form"],
                "name": "select-authenticator-authenticate",
                "href": "https://xyz.okta.com/idp/idx/challenge",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
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
              "cancel": {
                "rel": ["create-form"],
                "name": "cancel-polling",
                "href": "https://xyz.okta.com/idp/idx/authenticators/poll/cancel",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              },
              "contextualData": {
                "challenge": {
                  "type": "object",
                  "value": {
                    "challengeMethod": "LOOPBACK",
                    "challengeRequest": "eyJraWQ.challenge.jwt",
                    "domain": "http://localhost",
                    "probeTimeoutMillis": 3000,
                    "ports": ["8769", "65111", "65121"]
                  }
                }
              }
            }
          }
        }
        """;

    /// <summary>
    ///     "challenge-poll" response with a LOOPBACK device challenge (fast refresh for tests)
    /// </summary>
    public static string LoopbackChallenge(string challengeRequest = "eyJraWQ.challenge.jwt", int refresh = 20, params string[] ports)
    {
        var portList = string.Join(", ", (ports.Length == 0 ? ["8769", "65111"] : ports).Select(p => $"\"{p}\""));

        return $$"""
                 {
                   "version": "1.0.0",
                   "stateHandle": "02state-handle",
                   "intent": "LOGIN",
                   "remediation": {
                     "type": "array",
                     "value": [
                       {
                         "rel": ["create-form"],
                         "name": "challenge-poll",
                         "href": "https://xyz.okta.com/idp/idx/authenticators/poll",
                         "method": "POST",
                         "refresh": {{refresh}},
                         "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
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
                       "cancel": {
                         "rel": ["create-form"],
                         "name": "cancel-polling",
                         "href": "https://xyz.okta.com/idp/idx/authenticators/poll/cancel",
                         "method": "POST",
                         "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
                       },
                       "contextualData": {
                         "challenge": {
                           "type": "object",
                           "value": {
                             "challengeMethod": "LOOPBACK",
                             "challengeRequest": "{{challengeRequest}}",
                             "domain": "http://localhost",
                             "probeTimeoutMillis": 100,
                             "ports": [{{portList}}]
                           }
                         }
                       }
                     }
                   }
                 }
                 """;
    }

    /// <summary>
    ///     "device-challenge-poll" response with a CUSTOM_URI device challenge (fast refresh for tests)
    /// </summary>
    public static string CustomUriChallenge(int refresh = 20) =>
        DeviceChallengePollCustomUri.Replace("\"refresh\": 2000", $"\"refresh\": {refresh}");

    public const string DeviceChallengePollCustomUri =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "remediation": {
            "type": "array",
            "value": [
              {
                "rel": ["create-form"],
                "name": "device-challenge-poll",
                "relatesTo": ["$.authenticatorChallenge"],
                "href": "https://xyz.okta.com/idp/idx/authenticators/poll",
                "method": "POST",
                "refresh": 2000,
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              }
            ]
          },
          "authenticatorChallenge": {
            "type": "object",
            "value": {
              "challengeMethod": "CUSTOM_URI",
              "href": "com-okta-authenticator:/deviceChallenge?challengeRequest=eyJraWQ.custom.jwt",
              "downloadHref": "https://apps.apple.com/us/app/okta-verify/id490179405",
              "cancel": {
                "rel": ["create-form"],
                "name": "cancel-polling",
                "href": "https://xyz.okta.com/idp/idx/authenticators/poll/cancel",
                "method": "POST",
                "value": [ { "name": "stateHandle", "required": true, "value": "02state-handle", "visible": false, "mutable": false } ]
              }
            }
          }
        }
        """;

    public const string Success =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "success": {
            "rel": ["create-form"],
            "name": "success-redirect",
            "href": "https://xyz.okta.com/login/token/redirect?stateToken=02state-handle",
            "method": "GET"
          }
        }
        """;

    public const string InvalidCredentialsError =
        """
        {
          "version": "1.0.0",
          "stateHandle": "02state-handle",
          "intent": "LOGIN",
          "messages": {
            "type": "array",
            "value": [
              {
                "message": "Authentication failed",
                "i18n": { "key": "errors.E0000004" },
                "class": "ERROR"
              }
            ]
          }
        }
        """;
}
