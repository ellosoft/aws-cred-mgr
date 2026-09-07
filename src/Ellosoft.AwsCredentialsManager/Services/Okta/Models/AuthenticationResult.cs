// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models;

public record AuthenticationResult
{
    public required Uri OktaDomain { get; init; }

    public string? MfaUsed { get; init; }

    public string? StateToken { get; init; }

    public string? SessionId { get; init; }

    public string? SessionToken { get; init; }

    /// <summary>
    ///     Cookies set by Okta during an Identity Engine (FastPass) sign-in. In Identity Engine the session is carried by
    ///     several cookies (sid, idx, device token...), so subsequent Okta requests must send all of them, not only the session id
    /// </summary>
    public CookieContainer? SessionCookies { get; init; }

    public bool Authenticated { get; init; }

    /// <summary>
    ///     True when the result carries an Okta session: a session token (classic authentication)
    ///     or a session id (Identity Engine / FastPass authentication, where the session cookie is the session)
    /// </summary>
    public bool HasSession => Authenticated && (SessionToken is not null || SessionId is not null);
}

