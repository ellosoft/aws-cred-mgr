// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net.Http.Headers;
using System.Net.Http.Json;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("new")]
[Description("Create new credential profile")]
[Examples("new prod")]
public class CreateCredentialsProfile : AsyncCommand<CreateCredentialsProfile.Settings>
{
    public class Settings : AwsSettings
    {
        [CommandArgument(0, "<CREDENTIAL_NAME>")]
        [Description("Credential profile name")]
        public required string Name { get; set; }

        [CommandOption("--role-arn")]
        [Description("AWS role ARN")]
        public string? AwsRoleArn { get; set; }

        [CommandOption("-p|--aws-profile")]
        [Description("AWS profile to use (profile used in AWS CLI)")]
        [DefaultValue("default")]
        public string? AwsProfile { get; set; }

        [CommandOption("--okta-app-url")]
        [Description("URL of the AWS application in Okta")]
        public string? OktaAppUrl { get; set; }
    }

    private readonly IConfigManager _configManager;
    private readonly IOktaLoginService _oktaLogin;
    private readonly OktaSamlService _oktaSamlService;
    private readonly AwsSamlService _awsSamlService;

    public CreateCredentialsProfile(
        IConfigManager configManager,
        IOktaLoginService oktaLogin,
        OktaSamlService oktaSamlService,
        AwsSamlService awsSamlService)
    {
        _configManager = configManager;
        _oktaLogin = oktaLogin;
        _oktaSamlService = oktaSamlService;
        _awsSamlService = awsSamlService;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var oktaAppUrl = settings.OktaAppUrl ?? await GetAwsAppUrl(settings.OktaUserProfile);
        var awsRole = settings.AwsRoleArn ?? await GetAwsRoleArn(settings.OktaUserProfile, oktaAppUrl);

        CreateCredentials(settings, awsRole, oktaAppUrl);

        AnsiConsole.MarkupLine($"[bold green]'{settings.Name}' credentials created[/]");

        return 0;
    }

    private async Task<string> GetAwsAppUrl(string oktaUserProfile)
    {
        AnsiConsole.MarkupLine("[yellow]Retrieving AWS Apps from OKTA...[/]");

        var accessTokenResult = await _oktaLogin.InteractiveGetAccessToken(oktaUserProfile);

        if (accessTokenResult is null)
        {
            throw new CommandException(
                "Unable to create OKTA API Access Token, please try again or use the '--okta-app-url' option to specify an app URL manually");
        }

        var awsAppLinks = await GetAwsLinks(accessTokenResult.AuthResult.OktaDomain, accessTokenResult.AccessToken);

        if (awsAppLinks.Count == 0)
            throw new CommandException("Unable to find any AWS apps in Okta, please use the '--okta-app-url' option to specify an app URL manually");

        var appLink = AnsiConsole.Prompt(
            new SelectionPrompt<AppLink>()
                .Title("Select your [green]AWS Okta App[/]:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(app => app.Label)
                .AddChoices(awsAppLinks));

        return appLink.LinkUrl;

        static async Task<ICollection<AppLink>> GetAwsLinks(Uri oktaDomain, string accessToken)
        {
            using var httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var appLinks = await httpClient.GetFromJsonAsync(new Uri(oktaDomain, "/api/v1/users/me/appLinks"), OktaSourceGenerationContext.Default.ListAppLink);

            if (appLinks is not null)
                return appLinks.Where(app => app.AppName == "amazon_aws").ToList();

            throw new InvalidOperationException("Invalid Okta AppLinks response");
        }
    }

    private async Task<string> GetAwsRoleArn(string oktaUserProfile, string oktaAppUrl)
    {
        AnsiConsole.MarkupLine("[yellow]Retrieving AWS roles...[/]");

        var sessionTokenResult = await _oktaLogin.InteractiveLogin(oktaUserProfile);

        if (sessionTokenResult?.SessionToken is null)
            throw new CommandException("Unable to create AWS profile, please try again");

        var samlData = await _oktaSamlService.GetAppSamlDataAsync(sessionTokenResult.OktaDomain, oktaAppUrl,
            sessionTokenResult.SessionToken);

        var awsRoles = await _awsSamlService.GetAwsRolesWithAccountName(samlData);

        if (awsRoles.Count == 0)
            throw new CommandException("Unable to load AWS roles, please use the '--aws-role' option to specify an role manually");

        var rolesGroupedByAccount = awsRoles.GroupBy(k => k.AccountName, v => v.RoleName);

        var choices = new SelectionPrompt<string>()
            .Title("Select your AWS role:")
            .PageSize(10)
            .HighlightStyle(new Style(Color.Yellow, decoration: Decoration.Bold))
            .MoreChoicesText("[grey](Move up and down to reveal more options)[/]");

        choices.DisabledStyle = new Style(Color.Blue);

        foreach (var accountGroup in rolesGroupedByAccount)
            choices.AddChoiceGroup($"[blue]{accountGroup.Key}[/]", accountGroup);

        return AnsiConsole.Prompt(choices);
    }

    private void CreateCredentials(Settings settings, string awsRole, string oktaAppUrl)
    {
        var credential = new CredentialsConfiguration
        {
            AwsProfile = settings.AwsProfile!,
            RoleArn = awsRole,

            OktaAppUrl = oktaAppUrl,
            OktaProfile = settings.OktaUserProfile
        };

        _configManager.AppConfig.Credentials ??= new Dictionary<string, CredentialsConfiguration>();
        _configManager.AppConfig.Credentials[settings.Name] = credential;
        _configManager.SaveConfig();
    }
}
