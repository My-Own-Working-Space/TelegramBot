using System.Text;
using System.Text.Json;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class N8nIntegrationService(HttpClient httpClient, IConfiguration config, ILogger<N8nIntegrationService> logger) : IN8nIntegrationService
{
    private readonly string _n8nWebhookUrl = config["N8N_WEBHOOK_URL"] ?? "https://root-n8n-bot.duckdns.org/webhook/missing";

    public async Task<string> ForwardMessageAsync(string message, CancellationToken cancellationToken = default)
    {
        try
        {
            var payload = new { message };
            var content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

            var response = await httpClient.PostAsync(_n8nWebhookUrl, content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadAsStringAsync(cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error forwarding message to n8n");
            return $"Failed to reach n8n webhook: {ex.Message}";
        }
    }
}
