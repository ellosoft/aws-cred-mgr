// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("list"), Alias("ls")]
[Description("List all saved AWS RDS profiles")]
[Examples("ls")]
public class ListRdsProfiles : Command<AwsSettings>
{
    public override int Execute([NotNull] CommandContext context, [NotNull] AwsSettings settings)
    {
        return 0;
    }
}
