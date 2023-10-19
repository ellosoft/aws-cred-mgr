// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("list"), Alias("ls")]
[Description("List all saved credential profiles")]
[Examples("ls")]
public class ListCredentialsProfiles : Command<AwsSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] AwsSettings settings)
    {
        return 0;
    }
}
