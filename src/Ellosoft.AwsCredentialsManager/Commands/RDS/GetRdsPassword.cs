// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("get-password"), Alias("pwd")]
[Description("Get AWS RDS DB password for a RDS profile")]
[Examples("pwd prod_db")]
public class GetRdsPassword : Command<GetRdsPassword.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "[profile]")]
        [Description("RDS profile name (see: rds create)")]
        public string? Profile { get; set; }

        [CommandOption("-h|--host <HOST>")]
        [Description("DB instance endpoint")]
        public string? Hostname { get; set; }

        [CommandOption("-p|--port <PORT>")]
        [Description("Port number used for connecting to your DB instance")]
        public int Port { get; set; }

        [CommandOption("-u|--user-id <USER_ID>")]
        [Description("Database account that you want to access")]
        public string? UserId { get; set; }

        [CommandOption("-d|--database <DATABASE>")]
        [Description("Database name")]
        public string? Database { get; set; }

        [CommandOption("--ttl <PWD_LIFETIME>")]
        [Description("Password lifetime in minutes (max recommended: 15 minutes)")]
        [DefaultValue(15)]
        public int PasswordLifetime { get; set; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return 0;
    }
}
