using MyLinuxBot.Interfaces;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace MyLinuxBot.Commands;

public class ScanCommand(IJobScannerService jobScannerService) : ITelegramCommand
{
    public string CommandName => "/scan";
    public string Description => "Quét các job mới ngay lập tức.";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendMessage(message.Chat.Id, "Scanning for new jobs, please wait...", cancellationToken: cancellationToken);
        
        try
        {
            int count = await jobScannerService.ScanAndNotifyAsync(cancellationToken);
            
            if (count > 0)
            {
                await botClient.SendMessage(message.Chat.Id, $"Scan completed. Found {count} new jobs.", cancellationToken: cancellationToken);
            }
            else
            {
                await botClient.SendMessage(message.Chat.Id, "Scan completed. No new matching jobs found.", cancellationToken: cancellationToken);
            }
        }
        catch (Exception ex)
        {
            await botClient.SendMessage(message.Chat.Id, $"Error during scan: {ex.Message}", cancellationToken: cancellationToken);
        }
    }
}
