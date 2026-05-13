namespace MyLinuxBot.Interfaces;

public interface IWhisperService
{
    Task<string> GetTranscriptionAsync(string filePath, CancellationToken cancellationToken = default);
}
