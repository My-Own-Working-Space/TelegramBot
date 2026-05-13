using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Commands;

public class UploadCommand() : ITelegramCommand
{
    public string CommandName => "/upload";
    public string Description => "Uploads a document to ~/Downloads";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        if (message.Document is null)
        {
            await botClient.SendMessage(message.Chat.Id, "Please send a document to upload.", cancellationToken: cancellationToken);
            return;
        }

        var fileId = message.Document.FileId;
        var fileName = message.Document.FileName ?? "unknown_file";
        
        var downloadDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        Directory.CreateDirectory(downloadDir);
        var savePath = Path.Combine(downloadDir, fileName);

        var file = await botClient.GetFile(fileId, cancellationToken);
        
        if (file.FilePath == null)
        {
            await botClient.SendMessage(message.Chat.Id, "Could not get file path from telegram API.", cancellationToken: cancellationToken);
            return;
        }

        using var saveFileStream = new FileStream(savePath, FileMode.Create);
        await botClient.DownloadFile(file.FilePath, saveFileStream, cancellationToken);

        await botClient.SendMessage(message.Chat.Id, $"Successfully downloaded {fileName} to {savePath}", cancellationToken: cancellationToken);
    }
}
