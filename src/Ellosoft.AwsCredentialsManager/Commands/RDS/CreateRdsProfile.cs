// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("create"), Alias("new")]
[Description("Create new RDS profile")]
[Examples("create", "new")]
public class CreateRdsProfile : Command<AwsSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] AwsSettings settings)
    {
        return 0;
    }
}
