// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

namespace Ellosoft.AwsCredentialsManager.Services;

public static class Guard
{
    public static void AgainstChangeConfigWithVariable(string key, ResourceConfiguration config)
    {
        throw new NotSupportedException($"It is not possible to update a configuration having a variable. Config Key: {key}");
    }
}
