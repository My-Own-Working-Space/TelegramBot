using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class AiToolboxService(IShellService shellService, ILogger<AiToolboxService> logger) : IAiToolboxService
{
    public async Task<string> ExecuteSafeCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Toolbox executing safe command: {Command}", command);
        return await shellService.ExecuteCommandAsync(command, cancellationToken: cancellationToken);
    }

    public async Task<string> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Toolbox fetching system health.");
        try
        {
            var cpuRam = await shellService.ExecuteCommandAsync("top -bn1 | grep 'Cpu(s)\\|MiB Mem'", cancellationToken: cancellationToken);
            var disk = await shellService.ExecuteCommandAsync("df -h /", cancellationToken: cancellationToken);
            var uptime = await shellService.ExecuteCommandAsync("uptime -p", cancellationToken: cancellationToken);
            
            return $"==== SYSTEM HEALTH ====\nUptime: {uptime}\nCPU/RAM:\n{cpuRam}\nDisk:\n{disk}";
        }
        catch (Exception ex)
        {
            return $"Failed to get system health: {ex.Message}";
        }
    }

    public async Task<string> ReadLogSummaryAsync(string logFilePath, int lines = 100, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Toolbox reading log file: {FilePath}", logFilePath);
        try
        {
            // Sanitize log file path very basically
            var safePath = logFilePath.Replace("'", "").Replace(";", "").Replace("&", "");
            
            var command = $"tail -n {lines} '{safePath}' | grep -iE 'error|warn|fail|exception' | tail -n 20";
            var result = await shellService.ExecuteCommandAsync(command, cancellationToken: cancellationToken);
            
            if (string.IsNullOrWhiteSpace(result))
            {
                return $"No recent errors or warnings found in the last {lines} lines of {safePath}.";
            }
            
            return $"==== Log Issues from {safePath} ====\n{result}";
        }
        catch (Exception ex)
        {
            return $"Failed to read log summary: {ex.Message}";
        }
    }
}
