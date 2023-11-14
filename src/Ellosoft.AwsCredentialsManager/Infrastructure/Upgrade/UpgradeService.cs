// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics.CodeAnalysis;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade.Models;
using Ellosoft.AwsCredentialsManager.Services;
using Serilog;

namespace Ellosoft.AwsCredentialsManager.Infrastructure.Upgrade;

#pragma warning disable S1075 // URIs should not be hardcoded

public class UpgradeService
{
    private const string GITHUB_RELEASES_URL = "https://api.github.com/repos/ellosoft/aws-cred-mgr/releases";
    private const string GITHUB_LATEST_RELEASE_URL = "https://api.github.com/repos/ellosoft/aws-cred-mgr/releases/latest";

    private readonly ILogger _logger;
    private readonly IAnsiConsole _console;
    private readonly IAppMetadata _appMetadata;
    private readonly IFileDownloadService _downloadService;
    private readonly HttpClient _httpClient;

    public UpgradeService(ILogger logger) : this(
        logger,
        AnsiConsole.Console,
        new AppMetadata(),
        new FileDownloadService(),
        CreateHttpClient())
    {
    }

    public UpgradeService(
        ILogger logger,
        IAnsiConsole console,
        IAppMetadata appMetadata,
        IFileDownloadService downloadService,
        HttpClient httpClient)
    {
        _logger = logger.ForContext<UpgradeService>();
        _console = console;
        _appMetadata = appMetadata;
        _downloadService = downloadService;
        _httpClient = httpClient;
    }

    public async Task TryUpgradeApp()
    {
        try
        {
            var currentAppVersion = _appMetadata.GetAppVersion();

            if (currentAppVersion is null)
                return;

            var latestRelease = await GetLatestRelease(currentAppVersion);

            if (!TryGetDownloadAssetAndVersion(latestRelease, out var downloadUrl, out var latestVersion) || latestVersion <= currentAppVersion)
                return;

            var shouldUpdate = CheckIfUserWantsToUpdate(currentAppVersion, latestRelease);

            if (!shouldUpdate)
                return;

            using var zipStream = new MemoryStream();
            await _downloadService.DownloadFileAsync(_httpClient, downloadUrl, zipStream);

            var (executablePath, appFolder) = _appMetadata.GetExecutablePath();

            var executableName = Path.GetFileName(executablePath);
            var newFile = Path.Combine(appFolder, executableName + ".new");
            var archivePath = Path.Combine(Path.GetTempPath(), executableName + ".old");

            _console.MarkupLine("\r\nInstalling upgrade...");

            ExtractApp(zipStream, newFile);
            UpgradeApp(executablePath, archivePath, newFile);

            _console.MarkupLine("[green]Upgrade complete! The changes will reflect next time you execute the application.\r\n[/]");
        }
        catch (Exception e)
        {
            _logger.Error(e, "Unable to upgrade app");
            _console.MarkupLine("[yellow]Unable to upgrade app, try again later or " +
                                "download the new version from https://github.com/ellosoft/aws-cred-mgr/releases [/]");
        }
    }

    private async Task<GitHubRelease?> GetLatestRelease(SemanticVersion currentAppVersion)
    {
        if (!currentAppVersion.IsPreRelease)
            return await _httpClient.GetFromJsonAsync(GITHUB_LATEST_RELEASE_URL, GithubSourceGenerationContext.Default.GitHubRelease);

        var releases = await _httpClient.GetFromJsonAsync(GITHUB_RELEASES_URL, GithubSourceGenerationContext.Default.ListGitHubRelease);

        return releases?.Find(r => r.PreRelease);
    }

    private bool CheckIfUserWantsToUpdate(SemanticVersion currentAppVersion, GitHubRelease latestRelease)
    {
        var preReleaseTag = latestRelease.PreRelease ? "(Pre-release)" : String.Empty;

        _console.MarkupLine(
            $"""
             New version available:
             [b]Current Version:[/] {currentAppVersion}
             [b]New Version:[/] {latestRelease.Name} {preReleaseTag}
             [b]Release Notes:[/] {latestRelease.Url}

             """);

        return _console.Confirm("[yellow]Do you want to upgrade now ?[/]");
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

        var fileSuffix = $"{RuntimeInformation.RuntimeIdentifier}.zip";

        downloadUrl = latestRelease
            .Assets.FirstOrDefault(a => a.DownloadUrl.EndsWith(fileSuffix))?.DownloadUrl;

        return downloadUrl is not null;
    }

    private static void ExtractApp(Stream zipStream, string newFile)
    {
        using var archive = new ZipArchive(zipStream, ZipArchiveMode.Read);

        var mainAppEntry = archive.Entries.FirstOrDefault(e => e.Name == "aws-cred-mgr.exe");

        if (mainAppEntry is null)
            throw new InvalidOperationException("Unable to find application file in release archive");

        mainAppEntry.ExtractToFile(newFile, overwrite: true);
    }

    private static void UpgradeApp(string executablePath, string archivePath, string newFile)
    {
        if (File.Exists(archivePath))
            File.Delete(archivePath);

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
}
