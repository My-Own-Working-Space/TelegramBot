using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _modelUrl;
    private readonly IShellService _shellService;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient httpClient, IConfiguration config, IShellService shellService, ILogger<GeminiService> logger)
    {
        _httpClient = httpClient;
        _apiKey = config["GEMINI_API_KEY"] ?? "";
        _modelUrl = $"https://generativelanguage.googleapis.com/v1beta/models/gemini-1.5-flash:generateContent?key={_apiKey}";
        _shellService = shellService;
        _logger = logger;
    }

    public async Task<string> AskAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return "Error: GEMINI_API_KEY is not configured in .env file.";
        }

        try
        {
            var requestBody = new
            {
                contents = new object[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[] { new { text = prompt } }
                    }
                },
                tools = new object[]
                {
                    new
                    {
                        function_declarations = new object[]
                        {
                            new
                            {
                                name = "execute_shell_command",
                                description = "Executes a Linux shell command on the host machine and returns the output.",
                                parameters = new
                                {
                                    type = "object",
                                    properties = new
                                {
                                    command = new { type = "string", description = "The full shell command to execute." }
                                },
                                required = new[] { "command" }
                                }
                            }
                        }
                    }
                }
            };

            var response = await _httpClient.PostAsync(_modelUrl, 
                new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
                cancellationToken);

            response.EnsureSuccessStatusCode();
            var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

            var firstCandidate = geminiResponse?.Candidates?.FirstOrDefault();
            var firstPart = firstCandidate?.Content?.Parts?.FirstOrDefault();

            // Handle Function Call
            if (firstPart?.FunctionCall != null)
            {
                var functionCall = firstPart.FunctionCall;
                if (functionCall.Name == "execute_shell_command")
                {
                    _logger.LogInformation("Gemini requested shell execution: {Command}", functionCall.Arguments.Command);
                    var output = await _shellService.ExecuteCommandAsync(functionCall.Arguments.Command);
                    
                    // Send tool response back to Gemini to get final answer
                    return await SendToolResponseAsync(prompt, functionCall.Name, functionCall.Arguments.Command, output, cancellationToken);
                }
            }

            return firstPart?.Text ?? "Gemini returned an empty response.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            return $"Error: {ex.Message}";
        }
    }

    private async Task<string> SendToolResponseAsync(string originalPrompt, string functionName, string command, string output, CancellationToken cancellationToken)
    {
        var requestBody = new
        {
            contents = new object[]
            {
                new { role = "user", parts = new object[] { new { text = originalPrompt } } },
                new { role = "model", parts = new object[] { new { function_call = new { name = functionName, args = new { command = command } } } } },
                new { role = "function", parts = new object[] { new { function_response = new { name = functionName, response = new { content = output } } } } }
            }
        };
        
        var response = await _httpClient.PostAsync(_modelUrl, 
            new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json"), 
            cancellationToken);

        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync(cancellationToken);
        var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(jsonResponse);

        return geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text ?? "Error parsing final response.";
    }

    // JSON Mapping Classes
    private class GeminiResponse
    {
        [JsonPropertyName("candidates")]
        public Candidate[]? Candidates { get; set; }
    }

    private class Candidate
    {
        [JsonPropertyName("content")]
        public Content? Content { get; set; }
    }

    private class Content
    {
        [JsonPropertyName("parts")]
        public Part[]? Parts { get; set; }
    }

    private class Part
    {
        [JsonPropertyName("text")]
        public string? Text { get; set; }
        [JsonPropertyName("functionCall")]
        public FunctionCall? FunctionCall { get; set; }
    }

    private class FunctionCall
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }
        [JsonPropertyName("args")]
        public Arguments Arguments { get; set; } = new();
    }

    private class Arguments
    {
        [JsonPropertyName("command")]
        public string Command { get; set; } = "";
    }
}
