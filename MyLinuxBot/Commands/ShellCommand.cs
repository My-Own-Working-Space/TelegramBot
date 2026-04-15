using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Commands;

public class ShellCommand(IShellService shellService) : ITelegramCommand
{
    public string CommandName => "/shell";
    public string Description => "Executes a shell command.";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ', 2);
        if (parts == null || parts.Length < 2)
        {
            await botClient.SendMessage(message.Chat.Id, "Usage: /shell <cmd>", cancellationToken: cancellationToken);
            return;
        }

        var cmd = parts[1];
        var result = await shellService.ExecuteCommandAsync(cmd, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            result = "(no output)";

        if (result.Length > 4000)
            result = result[..4000] + "\n...[truncated]";
            
        result = System.Net.WebUtility.HtmlEncode(result);

        await botClient.SendMessage(message.Chat.Id, $"<pre>{result}</pre>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
    }
}
