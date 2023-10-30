// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Commands.AWS;

namespace Ellosoft.AwsCredentialsManager.Commands.RDS;

[Name("get-password"), Alias("pwd")]
[Description("Get AWS RDS DB password")]
[Examples(
    "pwd prod_db",
    "pwd -h localhost -p 5432 -u john -d postgres")]
public class GetRdsPassword : Command<GetRdsPassword.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "[PROFILE]")]
        [Description("RDS profile name (see: [italic blue]rds create[/], for instructions on how to create a new profile)")]
        public string? Profile { get; set; }

        [CommandOption("-h|--host")]
        [Description("DB instance endpoint")]
        public string? Hostname { get; set; }

        [CommandOption("-p|--port")]
        [Description("Port number used for connecting to your DB instance")]
        public int Port { get; set; }

        [CommandOption("-u|--user")]
        [Description("Database account that you want to access")]
        public string? UserId { get; set; }

        [CommandOption("-d|--database")]
        [Description("Database name")]
        public string? Database { get; set; }

        [CommandOption("--ttl")]
        [Description("Password lifetime in minutes (max recommended: 15 minutes)")]
        [DefaultValue(15)]
        public int PasswordLifetime { get; set; }
    }

    public override int Execute([NotNull] CommandContext context, [NotNull] Settings settings)
    {
        return 0;
    }
}
