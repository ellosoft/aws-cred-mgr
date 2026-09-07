// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

/// <summary>
///     Raised when an Okta FastPass (Identity Engine) sign-in cannot be completed. These are expected, user-facing
///     failures (Classic Engine org, Okta Verify missing, approval timeout...), hence a <see cref="CommandException" />
/// </summary>
public class OktaFastPassException(string message) : CommandException(message);
