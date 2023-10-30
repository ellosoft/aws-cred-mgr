// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models;

public record AccessTokenResult(string AccessToken, AuthenticationResult AuthResult);
