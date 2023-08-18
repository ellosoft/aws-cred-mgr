// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddAppLogging();

var registrar = new TypeRegistrar(services);
var app = new CommandApp(registrar);

app.Configure(config =>
{
    config.SetApplicationName("aws-cred-mgr");
    config.SetInterceptor(new LogInterceptor());

    config
        .AddBranch<CredentialsBranch, CommonSettings>(cred =>
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
    config.PropagateExceptions();
    //config.ValidateExamples();
#endif
});

if (Debugger.IsAttached)
{
    args = "rds pwd prod_db".Split(' ');
}

return app.Run(args);
