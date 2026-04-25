using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class GeminiService(IShellService shellService, ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly string PythonPath = "venv_bot/bin/python3";
    private static readonly string ScriptPath = "gemini_cli.py";

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Invoking Gemini CLI for prompt: {Prompt}", prompt);
            
            // Escape double quotes in prompt for shell execution
            var escapedPrompt = prompt.Replace("\"", "\\\"");
            var command = $"{PythonPath} {ScriptPath} \"{escapedPrompt}\"";
            
            var result = await shellService.ExecuteCommandAsync(command, cancellationToken: cancellationToken);
            
            if (string.IsNullOrWhiteSpace(result))
            {
                return "Gemini CLI returned no output.";
            }

            return result.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while calling Gemini CLI");
            return $"Error: {ex.Message}";
        }
    }
}
