using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger, 
    IConfiguration config,
    IServiceProvider serviceProvider) : IUpdateHandler
{
    private readonly long _allowedChatId = config.GetValue<long>("ALLOWED_CHAT_ID");

    public async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, HandleErrorSource source, CancellationToken cancellationToken)
    {
        var ErrorMessage = exception switch
        {
            ApiRequestException apiRequestException
                => $"Telegram API Error:\n[{apiRequestException.ErrorCode}]\n{apiRequestException.Message}",
            _ => exception.ToString()
        };

        logger.LogError(exception, "Bot error: {ErrorMessage}", ErrorMessage);
        await Task.CompletedTask;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type != UpdateType.Message || update.Message is not { } message)
            return;

        // Authorization Filter logic inside the handler
        if (message.Chat.Id != _allowedChatId)
        {
            logger.LogWarning("Unauthorized access attempt from ChatId: {ChatId}, Username: {Username}", message.Chat.Id, message.Chat.Username);
            return;
        }

        if (message.Text is not null)
        {
            if (message.Text.StartsWith('/'))
            {
                var parts = message.Text.Split(' ', 2);
                var commandName = parts[0].ToLowerInvariant(); // e.g., /shell
                
                using var cmdScope = serviceProvider.CreateScope();
                var commands = cmdScope.ServiceProvider.GetServices<ITelegramCommand>();
                var targetCommand = commands.FirstOrDefault(c => c.CommandName == commandName);

                if (targetCommand != null)
                {
                    try
                    {
                        logger.LogInformation("Executing command: {CommandName}", commandName);
                        await targetCommand.ExecuteAsync(botClient, message, cancellationToken);
                        return;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Error executing command {CommandName}", commandName);
                        await botClient.SendMessage(message.Chat.Id, "An error occurred.", cancellationToken: cancellationToken);
                        return;
                    }
                }
                // Fall through for unknown command to n8n AI
            }
            
            // Forward plain message or unknown command to n8n AI
            using var scope = serviceProvider.CreateScope();
            var n8nService = scope.ServiceProvider.GetRequiredService<IN8nIntegrationService>();
            
            var typingMsg = await botClient.SendMessage(message.Chat.Id, "Thinking...", cancellationToken: cancellationToken);
            
            var n8nResponse = await n8nService.ForwardMessageAsync(message.Text, cancellationToken);
            
            if (n8nResponse.Length > 4000)
                n8nResponse = n8nResponse[..4000] + "\n...[truncated]";
                
            var safeResponse = System.Net.WebUtility.HtmlEncode(n8nResponse);
            await botClient.EditMessageText(message.Chat.Id, typingMsg.MessageId, $"<pre>{safeResponse}</pre>", parseMode: ParseMode.Html, cancellationToken: cancellationToken);
        }
        else if (message.Type == MessageType.Document)
        {
            using var scope = serviceProvider.CreateScope();
            var commands = scope.ServiceProvider.GetServices<ITelegramCommand>();
            var uploadCommand = commands.FirstOrDefault(c => c.CommandName == "/upload");
            if (uploadCommand != null)
            {
                await uploadCommand.ExecuteAsync(botClient, message, cancellationToken);
            }
        }
    }
}
