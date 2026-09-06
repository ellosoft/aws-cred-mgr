// Copyright (c) 2026 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

/// <summary>
///     Raised when an Okta FastPass (Identity Engine) sign-in cannot be completed
/// </summary>
public class OktaFastPassException(string message, Exception? innerException = null) : Exception(message, innerException);
