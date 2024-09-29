// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Text;
using Ellosoft.AwsCredentialsManager;
using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Commands.Config;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Commands.Utils;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade;
using Ellosoft.AwsCredentialsManager.Services;
using Microsoft.Extensions.DependencyInjection;
using Serilog.Events;

Console.OutputEncoding = Encoding.UTF8;

var logger = LogRegistration.CreateNewLogger();

var upgradeService = new UpgradeService(logger);
var upgraded = await upgradeService.TryUpgradeApp();

if (upgraded)
    return 0;

var services = new ServiceCollection()
    .SetupLogging(logger)
    .AddAppServices();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName(AppMetadata.AppName);
    config.UseAssemblyInformationalVersion();

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

    // root commands
    config.AddCommand<OpenLogs>();

    config.PropagateExceptions();

#if DEBUG
    config.ValidateExamples();

    if (System.Diagnostics.Debugger.IsAttached)
        args = "okta setup".Split(' ');
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

    if (e is CommandRuntimeException && e.InnerException is not null)
    {
        e = e.InnerException;
    }

    if (logger.IsEnabled(LogEventLevel.Debug))
        AnsiConsole.WriteException(e);
    else
        AnsiConsole.MarkupLine($"[red bold]Error: [/]{e.Message}");
}

return -1;
