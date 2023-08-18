// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

public class InvalidUsernameOrPasswordException : Exception
{
    public InvalidUsernameOrPasswordException(Exception innerException) : base("Invalid username or password", innerException)
    {
    }
}
