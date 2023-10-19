// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddAppLogging();

services
    .AddSingleton<IOktaLoginService, OktaLoginService>()
    .AddSingleton<IConfigManager, ConfigManager>()
    .AddSingleton<AwsOktaSessionManager, AwsOktaSessionManager>();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("aws-cred-mgr");
    config.SetInterceptor(new LogInterceptor());

    config
        .AddBranch<CredentialsBranch, AwsSettings>(cred =>
        {
            cred.AddCommand<GetCredentials>();
            cred.AddCommand<ListCredentialsProfiles>();
            cred.AddCommand<CreateCredentialsProfile>();
        })
        .AddBranch<OktaBranch, CommonSettings>(okta =>
        {
            okta.AddCommand<SetupOkta>();
        })
        .AddBranch<RdsBranch, AwsSettings>(rds =>
        {
            rds.AddCommand<GetRdsPassword>();
            rds.AddCommand<ListRdsProfiles>();
            rds.AddCommand<CreateRdsProfile>();
        });

#if DEBUG
    //config.PropagateExceptions();
    //config.ValidateExamples();
#endif
});

if (Debugger.IsAttached)
{
    args = "cred new prod".Split(' ');
}

return app.Run(args);
