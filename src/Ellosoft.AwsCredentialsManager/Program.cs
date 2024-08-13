// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using Ellosoft.AwsCredentialsManager;
using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Commands.Config;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

var logger = LogRegistration.CreateNewLogger();

var upgradeService = new UpgradeService(logger);
await upgradeService.TryUpgradeApp();

var services = new ServiceCollection()
    .SetupLogging(logger)
    .RegisterAppServices();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("aws-cred-mgr");
    config.SetInterceptor(new LogInterceptor());

    config
        .AddBranch<OktaBranch>(okta =>
        {
            okta.AddCommand<SetupOkta>();
        })
        .AddBranch<CredentialsBranch>(cred =>
        {
            cred.AddCommand<GetCredentials>();
            cred.AddCommand<ListCredentialsProfiles>();
            cred.AddCommand<CreateCredentialsProfile>();
        })
        .AddBranch<RdsBranch>(rds =>
        {
            rds.AddCommand<GetRdsPassword>();
            rds.AddCommand<ListRdsProfiles>();
        })
        .AddBranch<ConfigBranch>(cfg =>
        {
            cfg.SetDefaultCommand<OpenConfig>();
            cfg.AddCommand<OpenConfig>();
            cfg.AddCommand<OpenAwsConfig>();
        });

    config.PropagateExceptions();

#if DEBUG
    config.ValidateExamples();

    if (Debugger.IsAttached)
        args = "cred new test1".Split(' ');
#endif
});

try
{
    return await app.RunAsync(args);
}
catch (CommandException e)
{
    AnsiConsole.MarkupLine($"[yellow bold]{e.Message}[/]");
}
catch (Exception e)
{
    logger.Error(e, "Unexpected error");

    if (logger.IsEnabled(LogEventLevel.Debug))
        AnsiConsole.WriteException(e);
    else
        AnsiConsole.MarkupLine($"[red bold]Error: [/]{e.Message}");
}

return -1;
