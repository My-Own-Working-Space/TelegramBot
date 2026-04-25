using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;

namespace MyLinuxBot.Services;

public class GeminiService(IShellService shellService, IAiToolboxService toolboxService, ILogger<GeminiService> logger) : IGeminiService
{
    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await AskWithHistoryAsync(new List<ChatMessage>(), prompt, cancellationToken);
    }
    
    public async Task<string> AskWithHistoryAsync(IEnumerable<ChatMessage> history, string currentPrompt, CancellationToken cancellationToken = default)
    {
        int maxLoops = 3;
        string promptToAppend = $"user: {currentPrompt}";
        string agentResponse = "";

        // Build base context
        var sb = new StringBuilder();
        sb.AppendLine("SYSTEM: You are Minh Chau Controller, a Linux Autonomous Agent.");
        sb.AppendLine("You MUST use your tools if asked to inspect the system, run commands, or check logs.");
        sb.AppendLine("To use a tool, reply EXACTLY with one of the following tags AND NO OTHER TEXT:");
        sb.AppendLine("[EXEC: <command>] - runs a bash command");
        sb.AppendLine("[HEALTH] - gets CPU/RAM/Disk health");
        sb.AppendLine("[LOGS: <file_path>] - summarizes error logs");
        sb.AppendLine("If you don't need a tool, just answer the user normally.");
        sb.AppendLine("--- CHAT HISTORY ---");
        
        foreach (var msg in history)
        {
            sb.AppendLine($"{msg.Role}: {msg.Content}");
        }

        for (int i = 0; i < maxLoops; i++)
        {
            sb.AppendLine(promptToAppend);
            string fullPrompt = sb.ToString();

            // Write to temp file to avoid bash escaping issues
            string tempFile = $"/tmp/gemini_prompt_{Guid.NewGuid():N}.txt";
            await File.WriteAllTextAsync(tempFile, fullPrompt, cancellationToken);

            try
            {
                // Pipe file to global gemini CLI 
                var command = $"cat '{tempFile}' | bash -c 'source ~/.nvm/nvm.sh && gemini'";
                agentResponse = await shellService.ExecuteCommandAsync(command, timeout: TimeSpan.FromSeconds(60), cancellationToken: cancellationToken);
                agentResponse = agentResponse.Trim();

                File.Delete(tempFile);

                logger.LogInformation("CLI Agent responded: {Response}", agentResponse);

                // ReAct Parser
                if (agentResponse.StartsWith("[EXEC:", StringComparison.OrdinalIgnoreCase))
                {
                    var cmd = agentResponse.Substring(6).TrimEnd(']').Trim();
                    var toolResult = await toolboxService.ExecuteSafeCommandAsync(cmd, cancellationToken);
                    promptToAppend = $"model: {agentResponse}\nsystem tool result: {toolResult}\nuser: Summarize the tool result for me.";
                    continue; // Loop again with the tool result
                }
                if (agentResponse.StartsWith("[HEALTH]", StringComparison.OrdinalIgnoreCase))
                {
                    var toolResult = await toolboxService.GetSystemHealthAsync(cancellationToken);
                    promptToAppend = $"model: {agentResponse}\nsystem tool result: {toolResult}\nuser: Summarize the system health for me.";
                    continue;
                }
                if (agentResponse.StartsWith("[LOGS:", StringComparison.OrdinalIgnoreCase))
                {
                    var path = agentResponse.Substring(6).TrimEnd(']').Trim();
                    var toolResult = await toolboxService.ReadLogSummaryAsync(path, 100, cancellationToken);
                    promptToAppend = $"model: {agentResponse}\nsystem tool result: {toolResult}\nuser: Summarize the logs for me.";
                    continue;
                }

                // If no tool was called, break out and return the final answer
                break; 
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to execute gemini cli.");
                if (File.Exists(tempFile)) File.Delete(tempFile);
                return $"Error executing gemini cli: {ex.Message}";
            }
        }

        return string.IsNullOrWhiteSpace(agentResponse) ? "Agent returned empty response." : agentResponse;
    }
}
