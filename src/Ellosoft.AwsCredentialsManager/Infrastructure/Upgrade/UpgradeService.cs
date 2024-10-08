// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade.Models;
using Ellosoft.AwsCredentialsManager.Services;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade;

#pragma warning disable S1075 // URIs should not be hardcoded

public class UpgradeService(
    ILogger logger,
    IAnsiConsole console,
    IAppMetadata appMetadata,
    IFileDownloadService downloadService,
    HttpClient httpClient)
{
    private const string GITHUB_RELEASES_URL = "https://api.github.com/repos/ellosoft/aws-cred-mgr/releases";
    private const string GITHUB_LATEST_RELEASE_URL = "https://api.github.com/repos/ellosoft/aws-cred-mgr/releases/latest";

    private readonly ILogger _logger = logger.ForContext<UpgradeService>();

    public UpgradeService(ILogger logger) : this(
        logger,
        AnsiConsole.Console,
        new AppMetadata(),
        new FileDownloadService(),
        CreateHttpClient())
    {
    }

    public async Task<bool> TryUpgradeApp()
    {
        try
        {
            if (!ShouldCheckForUpgrade())
                return false;

            var currentAppVersion = appMetadata.GetAppVersion();

            if (currentAppVersion is null)
                return false;

            var latestRelease = await GetLatestRelease(currentAppVersion);

            if (!TryGetDownloadAssetAndVersion(latestRelease, out var downloadUrl, out var latestVersion) || latestVersion <= currentAppVersion)
                return false;

            var shouldUpdate = CheckIfUserWantsToUpdate(currentAppVersion, latestRelease);

            if (!shouldUpdate)
                return false;

            var (executablePath, appFolder) = appMetadata.GetExecutablePath();

            var executableName = Path.GetFileName(executablePath);
            var newFilePath = Path.Combine(appFolder, executableName + ".new");
            var archivePath = Path.Combine(Path.GetTempPath(), executableName + ".old");

            await downloadService.DownloadAsync(httpClient, downloadUrl, newFilePath);

            console.MarkupLine("\r\nInstalling upgrade...");

            UpgradeApp(executablePath, archivePath, newFilePath);

            console.MarkupLine("Upgrade complete! The changes will reflect next time you execute the application.\r\n");
            console.MarkupLine("If you like this tool, consider giving it a Star:star: on GitHub, it's free and only takes 2 minutes!\r\n" +
                               "Link: https://github.com/ellosoft/aws-cred-mgr\r\n");

            return true;
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unable to upgrade app");
            console.MarkupLine("[yellow]Unable to upgrade app, try again later or " +
                               "download the new version from https://github.com/ellosoft/aws-cred-mgr/releases [/]");

            return false;
        }
    }

    private async Task<GitHubRelease?> GetLatestRelease(SemanticVersion currentAppVersion)
    {
        if (!currentAppVersion.IsPreRelease)
            return await httpClient.GetFromJsonAsync(GITHUB_LATEST_RELEASE_URL, GithubSourceGenerationContext.Default.GitHubRelease);

        var releases = await httpClient.GetFromJsonAsync(GITHUB_RELEASES_URL, GithubSourceGenerationContext.Default.ListGitHubRelease);

        return releases?.Find(r => r.PreRelease);
    }

    private bool CheckIfUserWantsToUpdate(SemanticVersion currentAppVersion, GitHubRelease latestRelease)
    {
        var preReleaseTag = latestRelease.PreRelease ? "(Pre-release)" : String.Empty;

        console.MarkupLine(
            $"""
             New version available:
             [b]Current Version:[/] {currentAppVersion}
             [b]New Version:[/] {latestRelease.Name} {preReleaseTag}
             [b]Release Notes:[/] {latestRelease.Url}

             """);

        return console.Confirm("[yellow]Do you want to upgrade now ?[/]");
    }

    private static bool TryGetDownloadAssetAndVersion(
        [NotNullWhen(true)] GitHubRelease? latestRelease,
        [NotNullWhen(true)] out string? downloadUrl,
        [NotNullWhen(true)] out SemanticVersion? version)
    {
        downloadUrl = null;
        version = null;

        if (latestRelease is null || latestRelease.Assets.Count == 0)
            return false;

        if (!SemanticVersion.TryParse(latestRelease.Name, out version))
            return false;

        downloadUrl = latestRelease
            .Assets.FirstOrDefault(a => a.DownloadUrl.Contains(RuntimeInformation.RuntimeIdentifier))?.DownloadUrl;

        return downloadUrl is not null;
    }

    private static void UpgradeApp(string executablePath, string archivePath, string newFile)
    {
        if (File.Exists(archivePath))
            File.Delete(archivePath);

        if (!OperatingSystem.IsWindows())
            File.SetUnixFileMode(newFile, UnixFileMode.UserRead | UnixFileMode.UserExecute);

        File.Move(executablePath, archivePath);
        File.Move(newFile, executablePath);
    }

    private static HttpClient CreateHttpClient()
    {
        const string USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:109.0) Gecko/20100101 Firefox/115.0";

        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("User-Agent", USER_AGENT);

        return httpClient;
    }

    private static bool ShouldCheckForUpgrade()
    {
        var upgradeCheck = AppDataDirectory.GetPath("version_check.txt");

        var lastUpgradeCheckDateUtc = ReadLastUpgradeCheckDate();

        if (lastUpgradeCheckDateUtc is null || lastUpgradeCheckDateUtc < DateTime.UtcNow.AddDays(-1))
        {
            File.WriteAllText(upgradeCheck, DateTime.UtcNow.ToBinary().ToString());

            return true;
        }

        return false;

        DateTime? ReadLastUpgradeCheckDate()
        {
            if (!File.Exists(upgradeCheck))
                return null;

            var lastUpgradeCheckValue = File.ReadAllText(upgradeCheck);

            if (!long.TryParse(lastUpgradeCheckValue, out var lastUpgradeCheckBinaryDateUtc))
                return null;

            return DateTime.FromBinary(lastUpgradeCheckBinaryDateUtc);
        }
    }
}
