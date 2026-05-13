using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyLinuxBot.Interfaces;

public interface ITelegramCommand
{
    string CommandName { get; }
    string Description { get; }
    Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken);
}
