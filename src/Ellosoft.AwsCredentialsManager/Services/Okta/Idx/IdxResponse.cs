// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Text.Json;
using System.Text.Json.Nodes;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

public sealed record IdxMessage(string Message, string? Key);

public sealed record IdxRemediation(string Name, string Href, int? Refresh, JsonArray? Fields);

/// <summary>
///     Device challenge issued by Okta for the Okta Verify "signed_nonce" (FastPass) method
/// </summary>
/// <param name="Method">LOOPBACK, CUSTOM_URI, UNIVERSAL_LINK, APP_LINK, ...</param>
/// <param name="ChallengeRequest">Challenge JWT (LOOPBACK)</param>
/// <param name="Domain">Loopback domain, e.g. http://localhost (LOOPBACK)</param>
/// <param name="Ports">Loopback ports to probe (LOOPBACK)</param>
/// <param name="ProbeTimeoutMillis">Timeout for each loopback probe (LOOPBACK)</param>
/// <param name="Href">Deep link that launches Okta Verify (CUSTOM_URI / UNIVERSAL_LINK / APP_LINK)</param>
public sealed record IdxDeviceChallenge(
    string Method,
    string? ChallengeRequest,
    string? Domain,
    IReadOnlyList<string> Ports,
    int? ProbeTimeoutMillis,
    string? Href)
{
    public const string Loopback = "LOOPBACK";
    public const string CustomUri = "CUSTOM_URI";

    public bool IsLoopback => Method == Loopback;
}

public sealed record IdxAuthenticatorOption(string Key, string Id, IReadOnlyList<string> MethodTypes, string? EnrollmentId);

/// <summary>
///     Read-only view over an Okta Identity Engine (IDX) Ion JSON response
/// </summary>
public sealed class IdxResponse
{
    public const string IdentifyRemediation = "identify";
    public const string ChallengeAuthenticatorRemediation = "challenge-authenticator";
    public const string SelectAuthenticatorRemediation = "select-authenticator-authenticate";
    public const string ChallengePollRemediation = "challenge-poll";
    public const string DeviceChallengePollRemediation = "device-challenge-poll";
    public const string LaunchAuthenticatorRemediation = "launch-authenticator";

    private const string ValueProperty = "value";

    private readonly JsonObject _root;

    private IdxResponse(JsonObject root)
    {
        _root = root;

        Remediations = ReadRemediations();
        RemediationNames = Remediations.Select(r => r.Name).ToList();
        ErrorMessages = ReadErrorMessages();
        AuthenticatorOptionLabels = GetAuthenticatorOptions().Select(o => o["label"]?.GetValue<string>() ?? string.Empty).ToList();
    }

    public static IdxResponse Parse(string json)
    {
        var node = JsonNode.Parse(json) as JsonObject ?? throw new InvalidOperationException("Invalid Okta Identity Engine response");

        return new IdxResponse(node);
    }

    public string? StateHandle => _root["stateHandle"]?.GetValue<string>();

    public bool IsSuccess => _root["success"] is JsonObject;

    public string? SuccessHref => _root["success"]?["href"]?.GetValue<string>();

    public IReadOnlyList<IdxRemediation> Remediations { get; }

    public IReadOnlyList<string> RemediationNames { get; }

    public bool HasRemediation(string name) => GetRemediation(name) is not null;

    public IdxRemediation? GetRemediation(string name) => Remediations.FirstOrDefault(r => r.Name == name);

    public IdxRemediation? PollRemediation =>
        GetRemediation(ChallengePollRemediation) ?? GetRemediation(DeviceChallengePollRemediation);

    public bool HasErrors => ErrorMessages.Count > 0;

    public IReadOnlyList<IdxMessage> ErrorMessages { get; }

    public bool IdentifyRequiresPassword =>
        GetRemediation(IdentifyRemediation)?.Fields?.OfType<JsonObject>().Any(f => f["name"]?.GetValue<string>() == "credentials") == true;

    public string? CurrentAuthenticatorKey => _root["currentAuthenticator"]?[ValueProperty]?["key"]?.GetValue<string>();

    /// <summary>
    ///     Device challenge for Okta Verify FastPass. Present with "challenge-poll" (currentAuthenticator.contextualData.challenge)
    ///     and "device-challenge-poll" (authenticatorChallenge) remediations
    /// </summary>
    public IdxDeviceChallenge? DeviceChallenge
    {
        get
        {
            var challenge = _root["currentAuthenticator"]?[ValueProperty]?["contextualData"]?["challenge"]?[ValueProperty] as JsonObject
                            ?? _root["authenticatorChallenge"]?[ValueProperty] as JsonObject;

            var method = challenge?["challengeMethod"]?.GetValue<string>();

            if (challenge is null || method is null)
                return null;

            return new IdxDeviceChallenge(
                Method: method,
                ChallengeRequest: challenge["challengeRequest"]?.GetValue<string>(),
                Domain: challenge["domain"]?.GetValue<string>(),
                Ports: (challenge["ports"] as JsonArray ?? []).OfType<JsonNode>().Select(p => p.ToString()).ToList(),
                ProbeTimeoutMillis: challenge["probeTimeoutMillis"]?.GetValue<int>(),
                Href: challenge["href"]?.GetValue<string>());
        }
    }

    public string? CancelPollingHref =>
        (_root["currentAuthenticator"]?[ValueProperty]?["cancel"]?["href"] ?? _root["authenticatorChallenge"]?[ValueProperty]?["cancel"]?["href"])
        ?.GetValue<string>();

    public IReadOnlyList<string> AuthenticatorOptionLabels { get; }

    /// <summary>
    ///     Finds an authenticator option (from the "select-authenticator-authenticate" remediation) by authenticator key
    ///     (e.g. okta_verify, okta_password)
    /// </summary>
    public IdxAuthenticatorOption? GetAuthenticatorOption(string authenticatorKey)
    {
        foreach (var option in GetAuthenticatorOptions())
        {
            var key = ResolveAuthenticatorKey(option);

            if (key != authenticatorKey)
                continue;

            var formFields = (option[ValueProperty]?["form"]?[ValueProperty] as JsonArray ?? []).OfType<JsonObject>().ToList();

            var id = FieldValue(formFields, "id");

            if (id is null)
                continue;

            var methodTypeField = formFields.FirstOrDefault(f => f["name"]?.GetValue<string>() == "methodType");

            List<string> methodTypes = [];

            if (methodTypeField?["options"] is JsonArray methodOptions)
                methodTypes = methodOptions.Select(o => o?[ValueProperty]?.GetValue<string>()).OfType<string>().ToList();
            else if (methodTypeField?[ValueProperty]?.GetValue<string>() is { } singleMethodType)
                methodTypes = [singleMethodType];

            return new IdxAuthenticatorOption(key, id, methodTypes, FieldValue(formFields, "enrollmentId"));
        }

        return null;
    }

    public override string ToString() => _root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });

    private IEnumerable<JsonObject> GetAuthenticatorOptions()
    {
        var authenticatorField = GetRemediation(SelectAuthenticatorRemediation)?.Fields?.OfType<JsonObject>()
            .FirstOrDefault(f => f["name"]?.GetValue<string>() == "authenticator");

        return (authenticatorField?["options"] as JsonArray ?? []).OfType<JsonObject>();
    }

    private string? ResolveAuthenticatorKey(JsonObject option)
    {
        // relatesTo is a JSON path like "$.authenticatorEnrollments.value[0]" or "$.authenticators.value[1]"
        var relatesTo = option["relatesTo"]?.GetValue<string>();

        if (relatesTo is not null)
        {
            var segments = relatesTo.TrimStart('$', '.').Split('.');

            if (segments.Length == 2 && segments[1].StartsWith("value[") && segments[1].EndsWith(']')
                && int.TryParse(segments[1][6..^1], out var index)
                && _root[segments[0]]?[ValueProperty] is JsonArray collection
                && index < collection.Count
                && collection[index]?["key"]?.GetValue<string>() is { } key)
            {
                return key;
            }
        }

        return option["label"]?.GetValue<string>() switch
        {
            "Okta Verify" => "okta_verify",
            "Password" or "Okta Password" => "okta_password",
            _ => null
        };
    }

    private static string? FieldValue(IEnumerable<JsonObject> fields, string name) =>
        fields.FirstOrDefault(f => f["name"]?.GetValue<string>() == name)?[ValueProperty]?.GetValue<string>();

    private IReadOnlyList<IdxMessage> ReadErrorMessages() =>
        (_root["messages"]?[ValueProperty] as JsonArray ?? [])
        .OfType<JsonObject>()
        .Where(m => m["class"]?.GetValue<string>() is null or "ERROR")
        .Select(m => new IdxMessage(m["message"]?.GetValue<string>() ?? string.Empty, m["i18n"]?["key"]?.GetValue<string>()))
        .ToList();

    private IReadOnlyList<IdxRemediation> ReadRemediations() =>
        (_root["remediation"]?[ValueProperty] as JsonArray ?? [])
        .OfType<JsonObject>()
        .Select(r => new IdxRemediation(
            Name: r["name"]?.GetValue<string>() ?? string.Empty,
            Href: r["href"]?.GetValue<string>() ?? string.Empty,
            Refresh: r["refresh"]?.GetValue<int>(),
            Fields: r[ValueProperty] as JsonArray))
        .ToList();
}
