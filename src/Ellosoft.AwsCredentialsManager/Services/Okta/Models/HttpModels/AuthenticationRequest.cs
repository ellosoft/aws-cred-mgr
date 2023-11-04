// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

public class AuthenticationRequest
{
    public required string Username { get; set; }

    public required string Password { get; set; }
}
