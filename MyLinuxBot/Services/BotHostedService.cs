using Telegram.Bot;
using Telegram.Bot.Polling;

namespace MyLinuxBot.Services;

public class BotHostedService(
    ILogger<BotHostedService> logger, 
    ITelegramBotClient botClient, 
    IUpdateHandler updateHandler) : IHostedService
{
    private CancellationTokenSource? _cts;

    public Task StartAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Starting Telegram bot service...");
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        
        botClient.StartReceiving(
            updateHandler: updateHandler,
            receiverOptions: new ReceiverOptions { AllowedUpdates = [] },
            cancellationToken: _cts.Token
        );
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Stopping Telegram bot service...");
        _cts?.Cancel();
        return Task.CompletedTask;
    }
}
