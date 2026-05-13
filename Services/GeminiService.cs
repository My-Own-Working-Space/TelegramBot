using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;

namespace MyLinuxBot.Services;

public partial class GeminiService(IShellService shellService, IAiToolboxService toolboxService, ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly Regex ToolCallRegex = new(@"\[(?<type>EXEC|LOGS|HEALTH)(?::\s*(?<content>.*?))?\]", RegexOptions.IgnoreCase);

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await AskWithHistoryAsync(new List<ChatMessage>(), prompt, cancellationToken);
    }
    
    public async Task<string> AskWithHistoryAsync(IEnumerable<ChatMessage> history, string currentPrompt, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("SYSTEM: You are Minh Chau Controller, a Linux Autonomous Agent.");
        sb.AppendLine("You MUST use your tools if asked to inspect the system, run commands, or check logs.");
        sb.AppendLine("To use a tool, reply EXACTLY with one of the following tags:");
        sb.AppendLine("[EXEC: <command>] - runs a bash command");
        sb.AppendLine("[HEALTH] - gets CPU/RAM/Disk health");
        sb.AppendLine("[LOGS: <file_path>] - summarizes error logs");
        sb.AppendLine("If you don't need a tool, just answer the user normally.");
        sb.AppendLine("--- CHAT HISTORY ---");
        
        foreach (var msg in history)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }
        sb.AppendLine($"user: {currentPrompt}");

        string fullPrompt = sb.ToString();
        string tempFile = Path.Combine(Path.GetTempPath(), $"gemini_prompt_{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(tempFile, fullPrompt, cancellationToken);

        try
        {
            var command = $"cat '{tempFile}' | gemini";
            var agentResponse = await shellService.ExecuteCommandAsync(command, timeout: TimeSpan.FromSeconds(120), cancellationToken: cancellationToken);
            agentResponse = agentResponse.Trim();

            logger.LogInformation("Agent responded: {Response}", agentResponse);

            var match = ToolCallRegex.Match(agentResponse);
            if (match.Success)
            {
                var toolType = match.Groups["type"].Value.ToUpperInvariant();
                var content = match.Groups["content"].Value.Trim();
                
                return toolType switch
                {
                    "EXEC" => await toolboxService.ExecuteSafeCommandAsync(content, cancellationToken),
                    "HEALTH" => await toolboxService.GetSystemHealthAsync(cancellationToken),
                    "LOGS" => await toolboxService.ReadLogSummaryAsync(content, 100, cancellationToken),
                    _ => "Unknown tool type."
                };
            }

            return string.IsNullOrWhiteSpace(agentResponse) ? "Agent returned empty response." : agentResponse;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Gemini CLI");
            return $"Error: {ex.Message}";
        }
        finally
        {
            if (File.Exists(tempFile)) 
                File.Delete(tempFile);
        }
    }
}
