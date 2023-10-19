// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class ResourceConfiguration
{
    internal Dictionary<string, ConfigMetadata> Metadata { get; } = new();

    internal bool HasVariables => Metadata.Count > 0;
}
