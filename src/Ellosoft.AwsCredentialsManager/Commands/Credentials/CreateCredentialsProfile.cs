// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("create"), Alias("new")]
[Description("Create new credential profile")]
[Examples("create")]
public class CreateCredentialsProfile : Command<CommonSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] CommonSettings settings)
    {
        return 0;
    }
}
