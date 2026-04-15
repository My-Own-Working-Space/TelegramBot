namespace MyLinuxBot.Interfaces;

public interface IGeminiService
{
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
}
