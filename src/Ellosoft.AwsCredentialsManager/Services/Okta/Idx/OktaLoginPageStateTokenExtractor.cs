// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using System.Globalization;
using System.Text.RegularExpressions;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Idx;

/// <summary>
///     Extracts the Identity Engine <c>stateToken</c> embedded in the Okta hosted sign-in page.
///     The Okta Sign-In Widget bootstraps its IDX transaction from this token, and so do we.
/// </summary>
public static partial class OktaLoginPageStateTokenExtractor
{
    public static string? Extract(string html)
    {
        var match = StateTokenRegex().Match(html);

        if (!match.Success)
            return null;

        var rawToken = match.Groups["token"].Value;

        // Classic Engine pages declare the variable with an empty value
        var token = UnescapeJavaScript(rawToken);

        return token.Length > 0 ? token : null;
    }

    // matches: var stateToken = '02...';  |  "stateToken":"02..."  |  stateToken: "02..."
    [GeneratedRegex("""stateToken\s*"?\s*[=:]\s*(?:"(?<token>(?:[^"\\]|\\.)*)"|'(?<token>(?:[^'\\]|\\.)*)')""", RegexOptions.CultureInvariant)]
    private static partial Regex StateTokenRegex();

    // Okta escapes non alphanumeric characters as \xNN (or \uNNNN) inside the script block
    [GeneratedRegex(@"\\x(?<hex>[0-9A-Fa-f]{2})|\\u(?<hex>[0-9A-Fa-f]{4})", RegexOptions.CultureInvariant)]
    private static partial Regex JavaScriptEscapeRegex();

    private static string UnescapeJavaScript(string value)
    {
        var unescaped = JavaScriptEscapeRegex().Replace(value, m =>
            ((char)int.Parse(m.Groups["hex"].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture)).ToString());

        return unescaped.Replace("\\-", "-").Replace("\\_", "_").Trim();
    }
}
