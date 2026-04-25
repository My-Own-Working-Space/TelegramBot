namespace MyLinuxBot.Interfaces;

public interface IN8nIntegrationService
{
    Task<string> ForwardMessageAsync(string message, CancellationToken cancellationToken = default);
}
