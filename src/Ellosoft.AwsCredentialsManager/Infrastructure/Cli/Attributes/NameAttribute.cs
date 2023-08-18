// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class NameAttribute : Attribute
{
    public string Name { get; }

    public NameAttribute(string name) => Name = name;
}
