// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Commands;
using Ellosoft.AwsCredentialsManager.Commands.Credentials;
using Ellosoft.AwsCredentialsManager.Commands.Okta;
using Ellosoft.AwsCredentialsManager.Commands.RDS;
using Ellosoft.AwsCredentialsManager.Infrastructure.Cli;
using Ellosoft.AwsCredentialsManager.Infrastructure.Logging;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection()
    .AddAppLogging();

services
    .AddSingleton<IConfigManager, ConfigManager>()
    .AddSingleton<CredentialsManager>()
    .AddSingleton<EnvironmentManager>();

// okta related services
services
    .AddSingleton<OktaClassicAuthenticator>()
    .AddSingleton<OktaClassicAccessTokenProvider>()
    .AddSingleton<IOktaLoginService, OktaLoginService>()
    .AddSingleton<IOktaMfaFactorSelector, OktaMfaFactorSelector>()
    .AddSingleton<AwsOktaSessionManager>()
    .AddSingleton<OktaSamlService>();

// aws related services
services
    .AddSingleton<RdsTokenGenerator>()
    .AddSingleton<AwsSamlService>();

services.AddKeyedSingleton(nameof(OktaHttpClientFactory), OktaHttpClientFactory.CreateHttpClient());

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
            //cred.AddCommand<GetCredentials>();
            cred.AddCommand<ListCredentialsProfiles>();
            cred.AddCommand<CreateCredentialsProfile>();
        })
        .AddBranch<RdsBranch>(rds =>
        {
            rds.AddCommand<GetRdsPassword>();
            //rds.AddCommand<ListRdsProfiles>();
        });

    config.PropagateExceptions();
    config.ValidateExamples();
});

try
{
    return app.Run(args);
}
catch (CommandException e)
{
    AnsiConsole.MarkupLine($"[yellow bold]{e.Message}[/]");
}
catch (Exception e)
{
    AnsiConsole.MarkupLine($"[red bold]Error: [/]{e.Message}");
}

return -1;
