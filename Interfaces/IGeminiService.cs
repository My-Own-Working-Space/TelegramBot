namespace MyLinuxBot.Interfaces;

public interface IGeminiService
{
    Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default);
    Task<string> AskWithHistoryAsync(IEnumerable<MyLinuxBot.Models.ChatMessage> history, string currentPrompt, CancellationToken cancellationToken = default);
}
