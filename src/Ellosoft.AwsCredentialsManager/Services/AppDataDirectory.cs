// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Services;

public static class AppDataDirectory
{
    private const string APP_DATA_DIR = ".aws_cred_mgr";

    /// <summary>
    ///     Gets the path to the application's data directory.
    /// </summary>
    /// <returns>The location of the application's data directory is the user's home directory combined with the ".aws_cred_mgr" subdirectory.</returns>
    public static string GetPath() => GetOrCreateAppDataDirectory();

    /// <summary>
    ///     Gets the full path to the specified file in the application's data directory.
    /// </summary>
    /// <param name="fileName">The name of the file to get the path for.</param>
    /// <returns>The full path to the specified file in the application's data directory.</returns>
    public static string GetPath(string fileName) => Path.Combine(GetOrCreateAppDataDirectory(), fileName);

    private static string GetOrCreateAppDataDirectory()
    {
        var userHomeDirectory = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        var appDataDirectory = Directory.CreateDirectory(Path.Combine(userHomeDirectory, APP_DATA_DIR));

        return appDataDirectory.FullName;
    }
}
