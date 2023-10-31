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
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddAppLogging();

services
    .AddSingleton<IConfigManager, ConfigManager>();

// okta related services
services
    .AddSingleton<OktaClassicAuthenticator, OktaClassicAuthenticator>()
    .AddSingleton<OktaClassicAccessTokenProvider, OktaClassicAccessTokenProvider>()
    .AddSingleton<IOktaLoginService, OktaLoginService>()
    .AddSingleton<IOktaMfaFactorSelector, OktaMfaFactorSelector>()
    .AddSingleton<AwsOktaSessionManager, AwsOktaSessionManager>()
    .AddSingleton<OktaSamlService, OktaSamlService>();

// aws related services
services
    .AddSingleton<RdsTokenGenerator, RdsTokenGenerator>()
    .AddSingleton<AwsSamlService, AwsSamlService>();

services.AddKeyedSingleton(nameof(OktaHttpClientFactory), OktaHttpClientFactory.CreateHttpClient());

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
        });

    config.PropagateExceptions();

#if DEBUG
    //config.ValidateExamples();
#endif
});

if (Debugger.IsAttached)
{
    args = "rds pwd".Split(' ');
}

try
{
    return app.Run(args);
}
catch (CommandException e)
{
    AnsiConsole.MarkupLine($"[red bold]{e.Message}[/]");
}
catch (Exception e)
{
    AnsiConsole.MarkupLine($"[red bold]Unexpected Error:[/]{e.Message}");
}

return -1;
