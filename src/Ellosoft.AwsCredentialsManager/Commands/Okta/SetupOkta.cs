// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Commands.Okta;

[Name("setup")]
[Description("Setup Okta authentication (All parameters are optional)")]
[Examples(
    "setup",
    "setup -d https://xyz.okta.com -u john --mfa push",
    "setup xyz_profile -d https://xyz.okta.com -u john --mfa push")]
public class SetupOkta(IOktaLoginService loginService, IConfigManager configManager) : AsyncCommand<SetupOkta.Settings>
{
    public class Settings : CommonSettings
    {
        [CommandArgument(0, "[PROFILE]")]
        [DefaultValue(OktaConfiguration.DefaultProfileName)]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        public string Profile { get; set; } = OktaConfiguration.DefaultProfileName;

        [CommandOption("-d|--domain")]
        [Description("Your organization Okta domain URL (e.g. https://xyz.okta.com)")]
        public string? OktaDomain { get; set; }

        [CommandOption("-u|--user")]
        [Description("Your Okta username")]
        public string? Username { get; set; }

        [CommandOption("--mfa")]
        [Description("Your prefered MFA type <push|totp (code)>")]
        public string? PreferredMfaType { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        AnsiConsole.MarkupLine("Okta Setup");

        var oktaDomain = GetOktaDomainUrl(settings);
        var username = settings.Username ?? await AnsiConsole.AskAsync<string>("Enter your [green]Okta[/] username:");
        var password = await AnsiConsole.PromptAsync(new TextPrompt<string>("Enter your [green]Okta[/] password:").Secret());

        AnsiConsole.WriteLine();

        var credentials = new UserCredentials(username, password);
        var preferredMfaType = settings.PreferredMfaType is not null ? OktaMfaFactorSelector.GetOktaMfaFactorCode(settings.PreferredMfaType) : null;

        var authResult = await loginService.Login(oktaDomain, credentials, preferredMfaType, userProfileKey: settings.Profile);

        if (!authResult.Authenticated)
            throw new CommandException("Unable to create profile, please try again");

        CreateOktaProfile(settings.Profile, oktaDomain.ToString(), authResult.MfaUsed);

        AnsiConsole.MarkupLine($"[bold green]All good, '{settings.Profile}' Okta profile created[/]");

        return 0;
    }

    private static Uri GetOktaDomainUrl(Settings settings)
    {
        const string URL_MESSAGE = "Enter your [green]Okta[/] domain URL (e.g. https://xyz.okta.com): [grey85][[https://]][/]";

        var oktaDomain = settings.OktaDomain ?? AnsiConsole.Ask<string>(URL_MESSAGE);

        if (!oktaDomain.StartsWith("https://"))
            oktaDomain = $"https://{oktaDomain}";

        while (!Uri.TryCreate(oktaDomain, UriKind.Absolute, out _))
        {
            AnsiConsole.MarkupLine("[red]Invalid URL, please try again[/]");
            oktaDomain = AnsiConsole.Ask<string>(URL_MESSAGE);
        }

        return new Uri(oktaDomain);
    }

    private void CreateOktaProfile(string profileName, string oktaDomain, string? preferredMfaType)
    {
        var appConfig = configManager.AppConfig;
        appConfig.Authentication ??= new AppConfig.AuthenticationSection();

        appConfig.Authentication.Okta[profileName] = new OktaConfiguration
        {
            OktaDomain = oktaDomain,
            PreferredMfaType = preferredMfaType
        };

        configManager.SaveConfig();
    }
}
