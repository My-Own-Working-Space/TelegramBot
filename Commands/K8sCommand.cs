using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Commands;

public class K8sCommand(IShellService shellService) : ITelegramCommand
{
    public string CommandName => "/k8s";
    public string Description => "Executes a kubernetes command in ~/Documents/Documents/K8s/Setup.";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var parts = message.Text?.Split(' ', 2);
        if (parts == null || parts.Length < 2)
        {
            await botClient.SendMessage(message.Chat.Id, "Usage: /k8s <cmd>", cancellationToken: cancellationToken);
            return;
        }

        var k8sCmd = parts[1];
        var fullPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents/Documents/K8s/Setup");
        
        var result = await shellService.ExecuteCommandAsync(k8sCmd, workingDirectory: fullPath, cancellationToken: cancellationToken);

        if (string.IsNullOrWhiteSpace(result))
            result = "(no output)";

        if (result.Length > 4000)
            result = result[..4000] + "\n...[truncated]";

        result = System.Net.WebUtility.HtmlEncode(result);

        await botClient.SendMessage(message.Chat.Id, $"<pre>{result}</pre>", parseMode: Telegram.Bot.Types.Enums.ParseMode.Html, cancellationToken: cancellationToken);
    }
}
