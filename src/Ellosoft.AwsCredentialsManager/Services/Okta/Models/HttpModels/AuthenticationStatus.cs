// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public static class AuthenticationStatus
{
    public const string Success = "SUCCESS";
    public const string MfaRequired = "MFA_CHALLENGE";
    public const string PasswordExpired = "PASSWORD_EXPIRED";
    public const string MfaEnroll = "MFA_ENROLL";
    public const string Unauthenticated = "UNAUTHENTICATED";
    public const string LockedOut = "LOCKED_OUT";
}
