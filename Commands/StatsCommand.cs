using Telegram.Bot;
using Telegram.Bot.Types;
using MyLinuxBot.Interfaces;
using Telegram.Bot.Types.Enums;

namespace MyLinuxBot.Commands;

public class StatsCommand(IShellService shellService) : ITelegramCommand
{
    public string CommandName => "/stats";
    public string Description => "Show system statistics (CPU, RAM, Disk, Temp)";

    public async Task ExecuteAsync(ITelegramBotClient botClient, Message message, CancellationToken cancellationToken)
    {
        await botClient.SendChatAction(message.Chat.Id, ChatAction.Typing, cancellationToken: cancellationToken);

        var cpuTask = shellService.ExecuteCommandAsync("top -bn1 | grep 'Cpu(s)' | awk '{print $2 + $4\"%\"}'", cancellationToken: cancellationToken);
        var memTask = shellService.ExecuteCommandAsync("free -m | awk 'NR==2{printf \"%.2f%% (%dMB/%dMB)\", $3*100/$2, $3, $2}'", cancellationToken: cancellationToken);
        var diskTask = shellService.ExecuteCommandAsync("df -h / | awk 'NR==2{print $5 \" (\" $3 \"/\" $2 \")\"}'", cancellationToken: cancellationToken);
        var tempTask = shellService.ExecuteCommandAsync("cat /sys/class/thermal/thermal_zone0/temp 2>/dev/null | awk '{print $1/1000\"°C\"}' || echo \"N/A\"", cancellationToken: cancellationToken);
        var uptimeTask = shellService.ExecuteCommandAsync("uptime -p", cancellationToken: cancellationToken);

        await Task.WhenAll(cpuTask, memTask, diskTask, tempTask, uptimeTask);

        var response = $"System Statistics\n\n" +
                       $"CPU: {await cpuTask}\n" +
                       $"RAM: {await memTask}\n" +
                       $"Disk: {await diskTask}\n" +
                       $"Temp: {await tempTask}\n" +
                       $"Uptime: {await uptimeTask}";

        await botClient.SendMessage(
            message.Chat.Id, 
            response, 
            parseMode: ParseMode.Markdown, 
            cancellationToken: cancellationToken);
    }
}
