// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

using System.Diagnostics;

namespace Ellosoft.AwsCredentialsManager.Services.IO;

public interface IFileManager
{
    void OpenFileUsingDefaultApp(string filePath);

    bool FileExists(string filePath);

    public byte[] ReadFile(string filePath);

    void SaveFile(string filePath, byte[] data);

    void DeleteFile(string filePath);
}

[ExcludeFromCodeCoverage]
public class FileManager : PlatformServiceSlim, IFileManager
{
    private readonly string _openCommand = ExecuteMultiPlatformCommand(
        win: () => "explorer",
        macos: () => "open",
        linux: () => "xdg-open");

    public void OpenFileUsingDefaultApp(string filePath) => Process.Start(_openCommand, $"\"{filePath}\"");

    public bool FileExists(string filePath) => File.Exists(filePath);

    public byte[] ReadFile(string filePath) => File.ReadAllBytes(filePath);

    public void SaveFile(string filePath, byte[] data) => File.WriteAllBytes(filePath, data);

    public void DeleteFile(string filePath) => File.Delete(filePath);
}
