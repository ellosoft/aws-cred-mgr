// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using IOPath = System.IO.Path;

namespace Ellosoft.AwsCredentialsManager.Services;

public static class AppDataDirectory
{
    private const string APP_DATA_DIR = ".aws_cred_mgr";

    /// <summary>
    ///     Gets the path to the application's data directory.
    /// </summary>
    /// <returns>The location of the application's data directory is the user's home directory combined with the ".aws_cred_mgr" subdirectory.</returns>
    public static string Path { get; } = GetOrCreateAppDataDirectory();

    /// <summary>
    ///     Get the path to the user's home directory.
    /// </summary>
    public static string UserProfileDirectory { get; } = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

    /// <summary>
    ///     Gets the full path to the specified file in the application's data directory.
    /// </summary>
    /// <param name="fileName">The name of the file to get the path for.</param>
    /// <param name="createDirectory">If true, the directory will be created if it doesn't exist.</param>
    /// <returns>The full path to the specified file in the application's data directory.</returns>
    public static string GetPath(string fileName, bool createDirectory = false)
    {
        var path = IOPath.Combine(Path, fileName);

        if (!createDirectory)
            return path;

        var directory = IOPath.GetDirectoryName(path);

        if (directory is not null)
        {
            Directory.CreateDirectory(directory);
        }

        return path;
    }

    private static string GetOrCreateAppDataDirectory()
    {
        var userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appDataDirectory = Directory.CreateDirectory(IOPath.Combine(userHomeDirectory, APP_DATA_DIR));

        return appDataDirectory.FullName;
    }
}
