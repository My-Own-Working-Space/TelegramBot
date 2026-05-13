using System.Net.Http.Json;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class WhisperService(HttpClient httpClient, ILogger<WhisperService> logger) : IWhisperService
{
    private class WhisperResponse
    {
        public string Text { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
    }

    public async Task<string> GetTranscriptionAsync(string filePath, CancellationToken cancellationToken = default)
    {
        try
        {
            using var content = new MultipartFormDataContent();
            var fileStream = File.OpenRead(filePath);
            content.Add(new StreamContent(fileStream), "file", Path.GetFileName(filePath));

            logger.LogInformation("Sending audio to Whisper API...");
            var response = await httpClient.PostAsync("/transcribe", content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<WhisperResponse>(cancellationToken: cancellationToken);
            return result?.Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to call Whisper API");
            throw;
        }
    }
}
