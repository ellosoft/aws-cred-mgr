// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Commands.Okta;

[Name("setup")]
[Description("Setup Okta authentication (All parameters are optional)")]
[Examples("setup")]
public class SetupOkta : AsyncCommand<SetupOkta.Settings>
{
    private readonly IOktaLoginService _loginService;

    public SetupOkta(IOktaLoginService loginService) => _loginService = loginService;

    public class Settings : CommonSettings
    {
        [CommandOption("-d|--domain <domain>")]
        [Description("Your organization Okta domain URL (e.g. https://xyz.okta.com)")]
        public string? OktaDomain { get; set; }

        [CommandOption("-u|--user <user>")]
        [Description("Your Okta username")]
        public string? Username { get; set; }

        [CommandOption("--mfa <mfa>")]
        [Description("Your prefered MFA type <push|totp (code)>")]
        public string? PreferredMfaType { get; set; }

        [CommandOption("--profile <profile>")]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        [AllowNull]
        [DefaultValue("default")]
        public string ProfileName { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[bold yellow]Okta Setup[/]");

        var oktaDomain = settings.OktaDomain ?? AnsiConsole.Ask<string>("Enter organization [green]Okta[/] domain URL (e.g. https://xyz.okta.com)");
        if (!Uri.TryCreate(oktaDomain, UriKind.Absolute, out _))
        {
            AnsiConsole.MarkupLine("[bold red]Invalid Okta domain URL[/]");
            return 1;
        }

        var username = settings.Username ?? AnsiConsole.Ask<string>("Enter your [green]Okta[/] username:");
        var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [green]Okta[/] password:").Secret());
        var credentials = new UserCredentials(username, password);

        var preferredMfaType = settings.PreferredMfaType is not null ?
            OktaMfaFactorSelector.GetOktaMfaFactorCode(settings.PreferredMfaType) : null;

        var session = await _loginService.Login(oktaDomain, credentials, preferredMfaType, userProfileKey: settings.ProfileName);

        if (session is null)
        {
            AnsiConsole.MarkupLine("[bold red]Unable to create profile, please try again[/]");
            return 1;
        }

        AnsiConsole.MarkupLine($"[bold green]All good, '{settings.ProfileName}' Okta created[/]");

        return 0;
    }
}
