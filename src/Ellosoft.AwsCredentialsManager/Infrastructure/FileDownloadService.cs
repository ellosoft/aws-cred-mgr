// Copyright (c) 2023 Ellosoft Limited. All rights reserved.

namespace Ellosoft.AwsCredentialsManager.Infrastructure;

public interface IFileDownloadService
{
    Task DownloadAsync(HttpClient httpClient, string downloadUrl, string destinationFilePath);
}

public class FileDownloadService : IFileDownloadService
{
    public async Task DownloadAsync(HttpClient httpClient, string downloadUrl, string destinationFilePath)
    {
        await using var destinationStream = new FileStream(destinationFilePath, FileMode.Create, FileAccess.Write, FileShare.None);

        await AnsiConsole.Progress()
            .HideCompleted(true)
            .AutoClear(true)
            .StartAsync(async ctx =>
            {
                var startingTask = ctx.AddTask("Starting download").IsIndeterminate();

                using var httpResponse = await httpClient.GetAsync(downloadUrl, HttpCompletionOption.ResponseHeadersRead);

                httpResponse.EnsureSuccessStatusCode();

                startingTask.StopTask();

                var totalBytes = httpResponse.Content.Headers.ContentLength ?? throw new InvalidOperationException("Invalid download size");

                var downloadTask = ctx.AddTask("Downloading", new ProgressTaskSettings { MaxValue = Convert.ToDouble(totalBytes) });

                await using var downloadStream = await httpResponse.Content.ReadAsStreamAsync();

                var buffer = new byte[16384];
                int bytesRead;

                while ((bytesRead = await downloadStream.ReadAsync(buffer)) > 0)
                {
                    await destinationStream.WriteAsync(buffer, 0, bytesRead);
                    downloadTask.Increment(bytesRead);
                }

                await destinationStream.FlushAsync();

                downloadTask.StopTask();
            });
    }
}
