using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Commands;

public class AskCommand(IGeminiService geminiService) : ITelegramCommand
{
    public string CommandName => "/ask";
    public string Description => "Asks a question via Gemini CLI.";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ', 2);
        if (parts == null || parts.Length < 2)
        {
            await botClient.SendMessage(message.Chat.Id, "Usage: /ask <prompt>", cancellationToken: cancellationToken);
            return;
        }

        var prompt = parts[1];
        var typingMessage = await botClient.SendMessage(message.Chat.Id, "Thinking...", cancellationToken: cancellationToken);

        var result = await geminiService.AskAsync(prompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            result = "(no output)";

        if (result.Length > 4000)
            result = result[..4000] + "\n...[truncated]";
            
        result = System.Net.WebUtility.HtmlEncode(result);

        await botClient.EditMessageText(
            chatId: message.Chat.Id, 
            messageId: typingMessage.MessageId, 
            text: $"<pre>{result}</pre>", 
            parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, 
            cancellationToken: cancellationToken);
    }
}
