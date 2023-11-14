// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Reflection;
using Ellosoft.AwsCredentialsManager.Infrastructure;

namespace Ellosoft.AwsCredentialsManager.Services;

public interface IAppMetadata
{
    public SemanticVersion? GetAppVersion();

    (string executablePath, string appFolder) GetExecutablePath();
}

public class AppMetadata : IAppMetadata
{
    public SemanticVersion? GetAppVersion()
    {
        var versionValue = Assembly.GetEntryAssembly()?
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
            .InformationalVersion;

        return SemanticVersion.TryParse(versionValue, out var semanticVersion) ? semanticVersion : null;
    }

    public (string executablePath, string appFolder) GetExecutablePath()
    {
        var executablePath = Environment.ProcessPath;
        var appFolder = Path.GetDirectoryName(executablePath);

        if (executablePath is null || appFolder is null)
            throw new InvalidOperationException("Unable to acquire executable path or location");

        return (executablePath, appFolder);
    }
}
