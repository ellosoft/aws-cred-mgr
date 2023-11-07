// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services.Okta.Exceptions;

public class OktaProfileNotFoundException : Exception
{
    public OktaProfileNotFoundException(string profile) : base(GetErrorMessage(profile))
    {
    }

    public static string GetErrorMessage(string profile)
    {
        var profileCommand = "aws-cred-mgr okta setup" + (profile == OktaConfiguration.DefaultProfileName ? null : $" {profile}");

        return $"No '{profile}' Okta profile found, please use [green]'{profileCommand}'[/] to create a new profile";
    }
}
