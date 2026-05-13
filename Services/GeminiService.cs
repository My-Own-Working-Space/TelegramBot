using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Net.Http.Headers;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;

namespace MyLinuxBot.Services;

public class GeminiService(
    HttpClient httpClient, 
    IShellService shellService, 
    IAiToolboxService toolboxService, 
    IConfiguration config,
    ILogger<GeminiService> logger) : IGeminiService
{
    private static readonly Regex ToolCallRegex = new(@"^\[(?<type>EXEC|LOGS|HEALTH|SCAN|SCREEN|STATS|PS|POWER)(?::\s*(?<content>[^\]]{1,500}))?\]$", RegexOptions.IgnoreCase | RegexOptions.Multiline);
    private readonly string _apiKey = config["GROQ_API_KEY"] ?? "";
    private readonly SecurityConfig _securityConfig = LoadSecurityConfig();

    private static SecurityConfig LoadSecurityConfig()
    {
        try {
            var json = File.ReadAllText("security_config.json");
            return JsonSerializer.Deserialize<SecurityConfig>(json) ?? new SecurityConfig();
        } catch { return new SecurityConfig(); }
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        return await AskWithHistoryAsync(new List<ChatMessage>(), prompt, cancellationToken);
    }
    
    public async Task<string> AskWithHistoryAsync(IEnumerable<ChatMessage> history, string currentPrompt, CancellationToken cancellationToken = default)
    {
        int loopCount = 0;
        var currentHistory = history.ToList();

        while (loopCount < _securityConfig.MaxLoops)
        {
            loopCount++;
            var response = await CallGroqApiAsync(currentHistory, currentPrompt, cancellationToken);
            
            if (string.IsNullOrWhiteSpace(response)) return "AI returned an empty response.";

            var match = ToolCallRegex.Match(response);
            if (match.Success)
            {
                var toolType = match.Groups["type"].Value.ToUpperInvariant();
                var content = match.Groups["content"].Value.Trim();
                
                logger.LogInformation("AI requested tool: {Type} (Loop {Loop})", toolType, loopCount);

                var toolResult = toolType switch
                {
                    "SCAN" => await toolboxService.ExecuteSafeCommandAsync("echo 'Scanning jobs...'", cancellationToken).ContinueWith(_ => "Scan tool triggered."), 
                    "STATS" => await toolboxService.GetSystemHealthAsync(cancellationToken),
                    "HEALTH" => await toolboxService.GetSystemHealthAsync(cancellationToken),
                    "SCREEN" => await shellService.ExecuteCommandAsync("scrot /tmp/screen.png && echo 'Screenshot taken.'", cancellationToken: cancellationToken),
                    "PS" => await shellService.ExecuteCommandAsync("ps aux", cancellationToken: cancellationToken),
                    "EXEC" => await toolboxService.ExecuteSafeCommandAsync(content, cancellationToken),
                    "LOGS" => await toolboxService.ReadLogSummaryAsync(content, 50, cancellationToken),
                    _ => "Unknown or restricted tool."
                };

                return toolResult; 
            }

            return response;
        }

        return "AI Agent exceeded maximum reasoning loops.";
    }

    private async Task<string> CallGroqApiAsync(IEnumerable<ChatMessage> history, string prompt, CancellationToken ct)
    {
        var messages = new List<object>
        {
            new { role = "system", content = "You are Minh Chau Controller, a minimalist Linux Autonomous Agent. " +
                                            "Rules: NO emojis. Professional. Use tool tags ONLY when necessary.\n" +
                                            "- [STATS]: System health.\n" +
                                            "- [SCAN]: Job search.\n" +
                                            "- [SCREEN]: Screenshot.\n" +
                                            "- [EXEC: <cmd>]: Bash command (must be in whitelist).\n" +
                                            "- [LOGS: <path>]: Read logs." }
        };

        foreach (var msg in history)
            messages.Add(new { role = msg.Role == "user" ? "user" : "assistant", content = msg.Content });
        
        messages.Add(new { role = "user", content = prompt });

        var requestBody = new { model = "llama-3.1-8b-instant", messages = messages, temperature = 0.1 };

        try
        {
            var request = new HttpRequestMessage(HttpMethod.Post, "https://api.groq.com/openai/v1/chat/completions");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

            var response = await httpClient.SendAsync(request, ct);
            if (!response.IsSuccessStatusCode) return $"Groq API Error: {response.StatusCode}";

            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
            return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()?.Trim() ?? "";
        }
        catch (Exception ex) { return $"AI Call Error: {ex.Message}"; }
    }
}
