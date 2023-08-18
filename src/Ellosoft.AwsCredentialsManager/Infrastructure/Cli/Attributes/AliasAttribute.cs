// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class AliasAttribute : Attribute
{
    public string[] Aliases { get; }

    public AliasAttribute(params string[] aliases) => Aliases = aliases;
}
