// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("list"), Alias("ls")]
[Description("List all saved credential profiles")]
[Examples("ls")]
public class ListCredentialsProfiles : Command<CommonSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] CommonSettings settings)
    {
        return 0;
    }
}
