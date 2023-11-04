// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Cli.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class ExamplesAttribute : Attribute
{
    public string[] Examples { get; }

    public ExamplesAttribute(params string[] examples) => Examples = examples;
}
