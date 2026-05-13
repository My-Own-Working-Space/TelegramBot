using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;
using Telegram.Bot.Types.Enums;

namespace MyLinuxBot.Commands;

public class ProcessCommand(IShellService shellService) : ITelegramCommand
{
    public string CommandName => "/top";
    public string Description => "Show top 10 processes by CPU usage";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatAction(message.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);

        var output = await shellService.ExecuteCommandAsync(
            "ps -eo pid,ppid,cmd,%mem,%cpu --sort=-%cpu | head -n 11", 
            cancellationToken: cancellationToken);

        var response = $"Top 10 Processes (by CPU)\n\n```\n{output}\n```";

        await botClient.SendMessage(
            message.Chat.Id,
            response,
            parseMode: ParseMode.Markdown,
            cancellationToken: cancellationToken);
    }
}
