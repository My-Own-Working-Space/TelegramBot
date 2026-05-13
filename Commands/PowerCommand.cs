using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;
using Telegram.Bot.Types.ReplyMarkups;
using Telegram.Bot.Types.Enums;

namespace MyLinuxBot.Commands;

public class PowerCommand : ITelegramCommand
{
    public string CommandName => "/power";
    public string Description => "Manage system power (Shutdown, Reboot, Sleep)";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        var keyboard = new InlineKeyboardMarkup(new[]
        {
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Reboot", "power_reboot"),
                InlineKeyboardButton.WithCallbackData("Shutdown", "power_shutdown")
            },
            new[]
            {
                InlineKeyboardButton.WithCallbackData("Sleep", "power_sleep")
            }
        });

        await botClient.SendMessage(
            message.Chat.Id,
            "Power Management\nSelect an action below:",
            replyMarkup: keyboard,
            cancellationToken: cancellationToken);
    }
}
