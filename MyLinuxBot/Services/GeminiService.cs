using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;

namespace MyLinuxBot.Services;

public class GeminiService(HttpClient httpClient, IConfiguration config, IAiToolboxService toolboxService, ILogger<GeminiService> logger) : IGeminiService
{
    private readonly string _apiKey = config["GEMINI_API_KEY"] ?? "";
    private readonly string _modelUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent";

    // Update signature to accept chat history
    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await AskWithHistoryAsync(new List<ChatMessage>(), prompt, cancellationToken);
    }
    
    // New method supporting history
    public async Task<string> AskWithHistoryAsync(IEnumerable<ChatMessage> history, string currentPrompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey) || _apiKey == "your_gemini_api_key_here")
        {
            return "Error: GEMINI_API_KEY is not configured in .env file. Autonomous Agent capabilities require this key.";
        }

        try
        {
            var url = $"{_modelUrl}?key={_apiKey}";
            
            var contents = new List<object>();
            
            // System prompt injection via dummy messages or supported systemInstruction 
            // We use history to build context
            foreach (var msg in history)
            {
                contents.Add(new
                {
                    role = msg.Role == "model" ? "model" : "user",
                    parts = new object[] { new { text = msg.Content } }
                });
            }
            
            contents.Add(new
            {
                role = "user",
                parts = new object[] { new { text = $"You are an expert Linux System Administrator and Autonomous Agent 'Minh Chau Controller'.\n{currentPrompt}" } }
            });

            var requestBody = new
            {
                contents = contents.ToArray(),
                tools = new object[]
                {
                    new
                    {
                        function_declarations = new object[]
                        {
                            new
                            {
                                name = "execute_safe_shell_command",
                                description = "Executes a safe Linux bash shell command. Use this to investigate the system, run deploy scripts, or download files.",
                                parameters = new { type = "object", properties = new { command = new { type = "string" } }, required = new[] { "command" } }
                            },
                            new
                            {
                                name = "get_system_health",
                                description = "Fetches a quick system health snapshot including CPU, RAM, Disk usage, and Uptime.",
                                parameters = new { type = "object", properties = new Dictionary<string, object>() }
                            },
                            new
                            {
                                name = "read_log_summary",
                                description = "Reads and summarizes errors from a specific log file.",
                                parameters = new { type = "object", properties = new { file_path = new { type = "string" } }, required = new[] { "file_path" } }
                            }
                        }
                    }
                }
            };

            var response = await httpClient.PostAsync(url, 
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

            var firstCandidate = geminiResponse?.Candidates?.FirstOrDefault();
            var firstPart = firstCandidate?.Content?.Parts?.FirstOrDefault();

            if (firstPart?.FunctionCall != null)
            {
                var functionCall = firstPart.FunctionCall;
                string functionName = functionCall.Name ?? "";
                logger.LogInformation("Agent decided to call tool: {ToolName}", functionName);
                
                string output = "";
                if (functionName == "execute_safe_shell_command")
                {
                    var cmd = functionCall.Arguments.Command;
                    output = await toolboxService.ExecuteSafeCommandAsync(cmd, cancellationToken);
                }
                else if (functionName == "get_system_health")
                {
                    output = await toolboxService.GetSystemHealthAsync(cancellationToken);
                }
                else if (functionName == "read_log_summary")
                {
                    var path = functionCall.Arguments.FilePath;
                    output = await toolboxService.ReadLogSummaryAsync(path ?? "/var/log/syslog", 100, cancellationToken);
                }

                // Make follow-up API call with the tool response
                return await SendToolResponseAsync(url, contents, functionName, functionCall.Arguments, output, cancellationToken);
            }

            return firstPart?.Text ?? "Gemini returned an empty response.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error calling Gemini Brain");
            return $"AI Processing Error: {ex.Message}";
        }
    }

    private async Task<string> SendToolResponseAsync(string url, List<object> originalContents, string functionName, Arguments args, string output, CancellationToken cancellationToken)
    {
        var contents = new List<object>(originalContents)
        {
            new { role = "model", parts = new object[] { new { function_call = new { name = functionName, args = args } } } },
            new { role = "function", parts = new object[] { new { function_response = new { name = functionName, response = new { content = output } } } } }
        };

        var requestBody = new { contents = contents.ToArray() };
        
        var response = await httpClient.PostAsync(url, 
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(await response.Content.ReadAsStringAsync(cancellationToken));

        return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "Error generating final answer over tool response.";
    }

    // JSON Mapping Classes
    private class GeminiResponse { [JsonPropertyName("candidates")] public Candidate[]? Candidates { get; set; } }
    private class Candidate { [JsonPropertyName("content")] public Content? Content { get; set; } }
    private class Content { [JsonPropertyName("parts")] public Part[]? Parts { get; set; } }
    private class Part { [JsonPropertyName("text")] public string? Text { get; set; } [JsonPropertyName("functionCall")] public FunctionCall? FunctionCall { get; set; } }
    private class FunctionCall { [JsonPropertyName("name")] public string? Name { get; set; } [JsonPropertyName("args")] public Arguments Arguments { get; set; } = new(); }
    private class Arguments { [JsonPropertyName("command")] public string Command { get; set; } = ""; [JsonPropertyName("file_path")] public string FilePath { get; set; } = ""; }
}
