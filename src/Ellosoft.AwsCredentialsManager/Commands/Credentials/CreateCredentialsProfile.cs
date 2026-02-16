// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net.Http.Json;
using Ellosoft.AwsCredentialsManager.Commands.AWS;
using Ellosoft.AwsCredentialsManager.Services.AWS;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Configuration.Models;
using Ellosoft.AwsCredentialsManager.Services.Okta;
using Ellosoft.AwsCredentialsManager.Services.Okta.Interactive;
using Ellosoft.AwsCredentialsManager.Services.Okta.Models.HttpModels;

namespace Ellosoft.AwsCredentialsManager.Commands.Credentials;

[Name("new")]
[Description("Create new credential profile")]
[Examples("new prod")]
public class CreateCredentialsProfile(
    ICredentialsManager credentialsManager,
    IOktaLoginService oktaLogin,
    IOktaSamlService oktaSamlService,
    IAwsSamlService awsSamlService)
    : AsyncCommand<CreateCredentialsProfile.Settings>
{
    private const string DEFAULT_AWS_PROFILE_VALUE = "[credential name]";

    public class Settings : AwsSettings
    {
        [CommandArgument(0, "<CREDENTIAL_NAME>")]
        [Description("Credential profile name")]
        public required string Name { get; set; }

        [CommandOption("--role-arn")]
        [Description("AWS role ARN")]
        public string? AwsRoleArn { get; set; }

        [CommandOption("--aws-profile")]
        [Description("AWS profile to use (profile used in AWS CLI)")]
        [DefaultValue(DEFAULT_AWS_PROFILE_VALUE)]
        public string? AwsProfile { get; set; }

        [CommandOption("--okta-app-url")]
        [Description("URL of the AWS application in Okta")]
        public string? OktaAppUrl { get; set; }

        [CommandOption("--okta-profile")]
        [Description("Local Okta profile name (Useful if you need to authenticate in multiple Okta domains)")]
        [DefaultValue("default")]
        public string OktaUserProfile { get; set; } = OktaConfiguration.DefaultProfileName;
    }

    public override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var oktaAppUrl = settings.OktaAppUrl ?? await GetAwsAppUrl(settings.OktaUserProfile);
        var awsRole = settings.AwsRoleArn ?? await GetAwsRoleArn(settings.OktaUserProfile, oktaAppUrl);
        var awsProfile = settings.AwsProfile is null or DEFAULT_AWS_PROFILE_VALUE ? null : settings.AwsProfile;

        credentialsManager.CreateCredential(
            name: settings.Name,
            awsProfile: awsProfile,
            awsRole: awsRole,
            oktaAppUrl: oktaAppUrl,
            oktaProfile: settings.OktaUserProfile);

        AnsiConsole.MarkupLine($"[bold green]'{settings.Name}' credentials created[/]");

        return 0;
    }

    private async Task<string> GetAwsAppUrl(string oktaUserProfile)
    {
        AnsiConsole.MarkupLine("Retrieving AWS Apps from OKTA...");

        var authenticationResult = await oktaLogin.InteractiveLogin(oktaUserProfile, createSession: true);

        if (authenticationResult?.SessionId is null)
            throw new CommandException("Unable to retrieve OKTA apps, please try again or use the '--okta-app-url' option to specify an app URL manually");

        var awsAppLinks = await GetAwsLinks(authenticationResult.OktaDomain, authenticationResult.SessionId);

        if (awsAppLinks.Count == 0)
            throw new CommandException("No AWS apps found in Okta, please use the '--okta-app-url' option to specify an app URL manually");

        var appLink = AnsiConsole.Prompt(
            new SelectionPrompt<AppLink>()
                .Title("Select your [green]AWS Okta App[/]:")
                .PageSize(10)
                .MoreChoicesText("[grey](Move up and down to reveal more options)[/]")
                .UseConverter(app => app.Label)
                .AddChoices(awsAppLinks));

        return appLink.LinkUrl;
    }

    private static async Task<ICollection<AppLink>> GetAwsLinks(Uri oktaDomain, string sessionId)
    {
        // TODO: Replace with HttpClientFactory client
        using var httpClient = new HttpClient();

        var request = new HttpRequestMessage(HttpMethod.Get, new Uri(oktaDomain, "/api/v1/users/me/appLinks"));
        request.Headers.Add("Cookie", $"sid={sessionId}");

        var httpResponse = await httpClient.SendAsync(request);
        httpResponse.EnsureSuccessStatusCode();

        var appLinks = await httpResponse.Content.ReadFromJsonAsync(
            OktaSourceGenerationContext.Default.ListAppLink);

        if (appLinks is not null)
            return appLinks.Where(app => app.AppName == "amazon_aws").ToList();

        throw new InvalidOperationException("Invalid Okta AppLinks response");
    }

    private async Task<string> GetAwsRoleArn(string oktaUserProfile, string oktaAppUrl)
    {
        AnsiConsole.MarkupLine("Retrieving AWS roles...");

        var sessionTokenResult = await oktaLogin.InteractiveLogin(oktaUserProfile);

        if (sessionTokenResult?.SessionToken is null)
            throw new CommandException("Unable to create AWS credential profile, please try again");

        var samlData = await oktaSamlService.GetAppSamlDataAsync(sessionTokenResult.OktaDomain, oktaAppUrl,
            sessionTokenResult.SessionToken);

        var awsRoles = await awsSamlService.GetAwsRolesWithAccountName(samlData);

        if (awsRoles.Count == 0)
            throw new CommandException("Unable to load AWS roles, please use the '--aws-role' option to specify a role manually");

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
}
