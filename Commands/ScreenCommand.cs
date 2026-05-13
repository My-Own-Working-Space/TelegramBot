using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;
using Telegram.Bot.Types.Enums;

namespace MyLinuxBot.Commands;

public class ScreenCommand(IShellService shellService, ILogger<ScreenCommand> logger) : ITelegramCommand
{
    public string CommandName => "/screen";
    public string Description => "Take a screenshot of the host machine";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatAction(message.Chat.Id, ChatAction.UploadPhoto, cancellationToken: cancellationToken);

        var filePath = Path.Combine(Path.GetTempPath(), $"screenshot_{DateTime.Now:yyyyMMddHHmmss}.png");
        
        // Use DISPLAY=:0 for the primary display. Scrot needs this.
        var result = await shellService.ExecuteCommandAsync($"DISPLAY=:0 scrot {filePath}", cancellationToken: cancellationToken);

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                await using var stream = System.IO.File.OpenRead(filePath);
                await botClient.SendPhoto(
                    message.Chat.Id,
                    photo: InputFile.FromStream(stream),
                    caption: "Host Screenshot",
                    cancellationToken: cancellationToken);
            }
            finally
            {
                System.IO.File.Delete(filePath);
            }
        }
        else
        {
            logger.LogWarning("Screenshot failed: {Result}", result);
            await botClient.SendMessage(
                message.Chat.Id,
                $"Failed to take screenshot.\nError: {result}",
                cancellationToken: cancellationToken);
        }
    }
}
