// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;

namespace Ellosoft.AwsCredentialsManager.Commands.Okta;

[Name("setup")]
[Description("Setup Okta authentication (All parameters are optional)")]
[Examples(
    "setup",
    "setup -d https://xyz.okta.com -u john --mfa push",
    "setup xyz_profile -d https://xyz.okta.com -u john --mfa push")]
public class SetupOkta : AsyncCommand<SetupOkta.Settings>
{
    public class Settings : CommonSettings
    {
        [CommandArgument(0, "[PROFILE]")]
        [DefaultValue(OktaConstants.DefaultProfileName)]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        public string Profile { get; set; } = OktaConstants.DefaultProfileName;

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

    private readonly IOktaLoginService _loginService;
    private readonly IConfigManager _configManager;

    public SetupOkta(IOktaLoginService loginService, IConfigManager configManager)
    {
        _loginService = loginService;
        _configManager = configManager;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        AnsiConsole.MarkupLine("[bold yellow]Okta Setup[/]");

        var oktaDomain = GetOktaDomainUrl(settings);
        var username = settings.Username ?? AnsiConsole.Ask<string>("Enter your [green]Okta[/] username:");
        var password = AnsiConsole.Prompt(new TextPrompt<string>("Enter your [green]Okta[/] password:").Secret());

        AnsiConsole.WriteLine();

        var credentials = new UserCredentials(username, password);
        var preferredMfaType = settings.PreferredMfaType is not null ? OktaMfaFactorSelector.GetOktaMfaFactorCode(settings.PreferredMfaType) : null;

        var authResult = await _loginService.Login(oktaDomain, credentials, preferredMfaType, userProfileKey: settings.Profile);

        if (!authResult.Authenticated)
            throw new CommandException("Unable to create profile, please try again");

        CreateOktaProfile(settings.Profile, oktaDomain.ToString(), authResult.MfaUsed);

        AnsiConsole.MarkupLine($"[bold green]All good, '{settings.Profile}' Okta profile created[/]");

        return 0;
    }

    private static Uri GetOktaDomainUrl(Settings settings)
    {
        const string URL_MESSAGE = "Enter your [green]Okta[/] domain URL (e.g. https://xyz.okta.com):";

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
        var appConfig = _configManager.AppConfig;
        appConfig.Authentication ??= new AppConfig.AuthenticationSection();
        appConfig.Authentication.Okta ??= new Dictionary<string, OktaConfiguration>();

        appConfig.Authentication.Okta[profileName] = new OktaConfiguration
        {
            OktaDomain = oktaDomain,
            PreferredMfaType = preferredMfaType
        };

        _configManager.SaveConfig();
    }
}
