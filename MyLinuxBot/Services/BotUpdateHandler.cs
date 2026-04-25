using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Data;
using MyLinuxBot.Models;
using Microsoft.EntityFrameworkCore;

namespace MyLinuxBot.Services;

public class BotUpdateHandler(
    ILogger<BotUpdateHandler> logger, 
    IConfiguration config,
    IDbContextFactory<BotDbContext> dbContextFactory,
    IServiceScopeFactory scopeFactory) : IUpdateHandler
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
                
                using var cmdScope = scopeFactory.CreateScope();
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
            
            // Fetch history for Stateful Memory
            using var scope = scopeFactory.CreateScope();
            using var dbContext = dbContextFactory.CreateDbContext();
            var geminiService = scope.ServiceProvider.GetRequiredService<IGeminiService>();
            
            var history = await dbContext.ChatMessages
                            .Where(m => m.ChatId == message.Chat.Id)
                            .OrderByDescending(m => m.Id)
                            .Take(10)
                            .ToListAsync(cancellationToken);
            history.Reverse(); // Restore chronological order

                            
            // Save user message to memory
            var userMsg = new ChatMessage { ChatId = message.Chat.Id, Role = "user", Content = message.Text };
            dbContext.ChatMessages.Add(userMsg);
            await dbContext.SaveChangesAsync(cancellationToken);

            var typingMsg = await botClient.SendMessage(message.Chat.Id, "Thinking & Analyzing...", cancellationToken: cancellationToken);
            
            var response = await geminiService.AskWithHistoryAsync(history, message.Text, cancellationToken);
            
            // Save assistant response to memory
            var modelMsg = new ChatMessage { ChatId = message.Chat.Id, Role = "model", Content = response };
            dbContext.ChatMessages.Add(modelMsg);
            await dbContext.SaveChangesAsync(cancellationToken);
            
            if (response.Length > 4000)
                response = response[..4000] + "\n...[truncated]";
                
            await botClient.EditMessageText(message.Chat.Id, typingMsg.MessageId, response, parseMode: ParseMode.None, cancellationToken: cancellationToken);
        }
        else if (message.Type == MessageType.Document)
        {
            using var scope = scopeFactory.CreateScope();
            var commands = scope.ServiceProvider.GetServices<ITelegramCommand>();
            var uploadCommand = commands.FirstOrDefault(c => c.CommandName == "/upload");
            if (uploadCommand != null)
            {
                await uploadCommand.ExecuteAsync(botClient, message, cancellationToken);
            }
        }
    }
}
