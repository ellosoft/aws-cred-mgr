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

    public class Settings : AwsSettings
    {
        [CommandArgument(0, "<CREDENTIAL_NAME>")]
        [Description("Credential profile name")]
        public required string Name { get; set; }

        [CommandOption("--okta-app-url")]
        [Description("URL of the AWS application in Okta")]
        public string? OktaAppUrl { get; set; }

        [CommandOption("--aws-role")]
        [Description("AWS role ARN")]
        public string? AwsRoleArn { get; set; }

        [CommandOption("-p|--aws-profile")]
        [Description("AWS profile to use (profile used in AWS CLI)")]
        [DefaultValue("default")]
        public string? AwsProfile { get; set; }
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings)
    {
        var oktaAppUrl = settings.OktaAppUrl ?? await GetAwsAppUrl(settings.OktaUserProfile);

        if (oktaAppUrl is null)
            return 1;

        var awsRole = settings.AwsRoleArn ?? await GetAwsRoleArn(settings.OktaUserProfile, oktaAppUrl);

        if (awsRole is null)
            return 1;

        var credential = new CredentialsConfiguration
        {
            AwsProfile = settings.AwsProfile,
            Region = settings.Region?.DisplayName,
            RoleArn = awsRole,

            OktaAppUrl = oktaAppUrl,
            OktaProfile = settings.OktaUserProfile
        };

        _configManager.AppConfig.Credentials ??= new Dictionary<string, CredentialsConfiguration>();
        _configManager.AppConfig.Credentials[settings.Name] = credential;

        return 0;
    }

    private async Task<string?> GetAwsAppUrl(string oktaUserProfile)
    {
        AnsiConsole.MarkupLine("Retrieving AWS Apps from OKTA...");

        var accessTokenResult = await _oktaLogin.InteractiveGetAccessToken(oktaUserProfile);

        if (accessTokenResult is null)
            return null;

        var awsAppLinks = await GetAwsLinks(accessTokenResult.AuthResult.OktaDomain, accessTokenResult.AccessToken);

        if (awsAppLinks.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Unable to find any AWS apps in Okta, please use the '--okta-app-url' option to specify an URL manually[/]");

            return null;
        }

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

    private async Task<string?> GetAwsRoleArn(string oktaUserProfile, string oktaAppUrl)
    {
        var sessionTokenResult = await _oktaLogin.InteractiveLogin(oktaUserProfile);

        if (sessionTokenResult?.SessionToken is null)
            return null;

        var samlData = await _oktaSamlService.GetAppSamlDataAsync(sessionTokenResult.OktaDomain, oktaAppUrl,
            sessionTokenResult.SessionToken);

        var awsRoles = await _awsSamlService.GetAwsRolesWithAccountName(samlData);

        if (awsRoles.Count == 0)
        {
            AnsiConsole.MarkupLine("[red]Unable to load AWS roles, please use the '--aws-role' option to specify an role manually[/]");

            return null;
        }

        var sortedRoles = awsRoles.OrderBy(role => role.Value);

        var awsRole = AnsiConsole.Prompt(
            new SelectionPrompt<KeyValuePair<string, string>>()
                .Title("Select your AWS role:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(role => $"{role.Value}: {role.Key}")
                .AddChoices(sortedRoles));

        return awsRole.Key;
    }
}
