// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("new")]
[Description("Create new credential profile")]
[Examples("new")]
public class CreateCredentialsProfile : AsyncCommand<CreateCredentialsProfile.Settings>
{
    private readonly IConfigManager _configManager;

#pragma warning disable S4487

    private readonly IOktaLoginService _oktaLogin;

    public CreateCredentialsProfile(IConfigManager configManager, IOktaLoginService oktaLogin)
    {
        _configManager = configManager;
        _oktaLogin = oktaLogin;
    }

    public class Settings : AwsSettings
    {
        [CommandArgument(0, "<CREDENTIAL_NAME>")]
        [Description("Credential profile name")]
        public string? Name { get; set; }

        [CommandOption("-l|--okta-app-url <OKTA_APP_URL>")]
        [Description("Url of the AWS application in Okta")]
        [DefaultValue("default")]
        public string? OktaAppUrl { get; set; }

        [CommandOption("-p|--aws-profile <AWS_PROFILE>")]
        [Description("AWS profile to use (profile used in AWS CLI)")]
        [DefaultValue("default")]
        public string? AwsProfile { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        //var sessionToken = _oktaLogin.InteractiveLogin(settings.OktaUserProfile);

        //if (sessionToken is null)
        //    return 1;

        var localUrl = "http://localhost:8888/";

        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();
        builder.WebHost.UseUrls(localUrl);
        var app = builder.Build();

        var html = "<h1>Welcome to Code Maze</h1>";
        app.MapGet("/", async ctx =>
        {
            ctx.Response.ContentType = "text/html";
            await ctx.Response.WriteAsync(html);
        });

        await app.StartAsync();

        AnsiConsole.WriteLine("Started....");
        var info = new ProcessStartInfo(localUrl) { UseShellExecute = true };

        Process.Start(info);
        AnsiConsole.Confirm("Test");

        await app.StopAsync();

        AnsiConsole.WriteLine("Stopped...");

        _configManager.AppConfig.Credentials?.Add("test", new CredentialsConfiguration());

        return 0;
    }
}
