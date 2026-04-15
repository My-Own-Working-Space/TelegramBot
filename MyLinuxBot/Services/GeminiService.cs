using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class GeminiService(IShellService shellService) : IGeminiService
{
    // Assuming python environment has the required packages
    private const string GeminiCliPath = "~/Documents/gemini_cli.py";

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var escapedPrompt = prompt.Replace("\"", "\\\""); 
        var command = $"python3 {GeminiCliPath} \"{escapedPrompt}\"";
        
        return await shellService.ExecuteCommandAsync(command, timeout: TimeSpan.FromMinutes(1), cancellationToken: cancellationToken);
    }
}
