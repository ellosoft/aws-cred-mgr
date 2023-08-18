// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Serilog.Events;

namespace Ellosoft.AwsCredentialsManager.Commands;

public class CommonSettings : CommandSettings, ILogLevelSettings
{
    [CommandOption("--log-level <log_level>")]
    [Description("Set log level <verbose|debug|info|warn|error>")]
    [TypeConverter(typeof(LogLevelConverter))]
    [DefaultValue("warn")]
    public LogEventLevel LogLevel { get; set; }

    [CommandOption("--okta-user-profile <okta_user_profile>")]
    [Description("Override the Okta profile (this only applicable for saving user credentials)")]
    [DefaultValue("default")]
    public string OktaUserProfile { get; set; } = "default";
}
