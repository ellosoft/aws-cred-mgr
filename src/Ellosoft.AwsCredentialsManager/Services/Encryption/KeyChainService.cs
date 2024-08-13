// Copyright (c) 2024 Ellosoft Limited. All rights reserved.

using System.Runtime.Versioning;
using CliWrap;
using CliWrap.Buffered;

namespace Ellosoft.AwsCredentialsManager.Services.Encryption;

[SupportedOSPlatform("macos")]
public class KeyChainService
{
    private const string KEY_CHAIN_CLI = "security";

    public async Task<string?> GetSecureNote(string serviceName, string? account = null)
    {
        var accountName = GetAccountName(account);

        var cmdResult = await ExecuteCliCommand([
            "find-generic-password",
            "-a", accountName,
            "-s", serviceName,
            "-w"
        ]);

        if (cmdResult.IsSuccess)
        {
            var secureNote = cmdResult.StandardOutput.Trim();

            return secureNote;
        }

        if (!string.IsNullOrEmpty(cmdResult.StandardError))
        {
            Console.WriteLine($"Error: {cmdResult.StandardError}");
        }

        return null;
    }

    public async Task<bool> AddSecureNote(string serviceName, string content, string? executablePath,
        string? account = null)
    {
        var accountName = GetAccountName(account);

        var arguments = new List<string>
        {
            "add-generic-password",
            "-D", "secure note",
            "-a", accountName,
            "-s", serviceName,
            "-w", content,
            "-T", "/usr/bin/security",
            "-U"
        };

        if (executablePath is not null)
        {
            arguments.Add("-T");
            arguments.Add(executablePath);
        }

        var cmdResult = await ExecuteCliCommand(arguments);

        LogResult(cmdResult);

        return cmdResult.IsSuccess;
    }

    public async Task<bool> RemoveSecureNote(string serviceName, string? account = null)
    {
        var accountName = GetAccountName(account);

        var cmdResult = await ExecuteCliCommand([
            "delete-generic-password",
            "-a", accountName,
            "-s", serviceName
        ]);

        LogResult(cmdResult);

        return cmdResult.IsSuccess;
    }

    private static string GetAccountName(string? account = null) => account ?? Environment.UserName;

    private static void LogResult(BufferedCommandResult cmdResult)
    {
        Console.WriteLine($"Exit Code: {cmdResult.ExitCode}");
        Console.WriteLine($"Standard Output: {cmdResult.StandardOutput}");
        Console.WriteLine($"Standard Error: {cmdResult.StandardError}");
        Console.WriteLine($"Execution Time: {cmdResult.RunTime}");
    }

    private static async Task<BufferedCommandResult> ExecuteCliCommand(IEnumerable<string> arguments) =>
        await Cli.Wrap(KEY_CHAIN_CLI)
            .WithArguments(arguments)
            .WithValidation(CommandResultValidation.None)
            .ExecuteBufferedAsync();
}
