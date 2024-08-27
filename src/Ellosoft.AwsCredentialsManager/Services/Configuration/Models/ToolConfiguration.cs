// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services.Configuration.Models;

public class ToolConfiguration : ResourceConfiguration
{
    public bool? CopyToClipboard { get; set; }

    public bool? AwsIgnoreConfiguredEndpoints { get; set; }
}

public class ActiveToolConfiguration(ToolConfiguration config)
{
    public bool CopyToClipboard => config.CopyToClipboard ?? true;

    public bool AwsIgnoreConfiguredEndpoints => config.AwsIgnoreConfiguredEndpoints ?? true;
}
