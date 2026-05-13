using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class AiToolboxService(IShellService shellService, ILogger<AiToolboxService> logger) : IAiToolboxService
{
    private static readonly string[] AllowedLogDirectories = { "/var/log/" };

    public async Task<string> ExecuteSafeCommandAsync(string command, CancellationToken cancellationToken = default)
    {
        // Security is now centralized in ShellService via Whitelist
        return await shellService.ExecuteCommandAsync(command, cancellationToken: cancellationToken);
    }

    public async Task<string> GetSystemHealthAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Toolbox fetching system health.");
        try
        {
            var cpuRamTask = shellService.ExecuteCommandAsync("top -bn1 | grep 'Cpu(s)\\|MiB Mem'", cancellationToken: cancellationToken);
            var diskTask = shellService.ExecuteCommandAsync("df -h /", cancellationToken: cancellationToken);
            var uptimeTask = shellService.ExecuteCommandAsync("uptime -p", cancellationToken: cancellationToken);
            
            await Task.WhenAll(cpuRamTask, diskTask, uptimeTask);
            
            return $"==== SYSTEM HEALTH ====\nUptime: {await uptimeTask}\nCPU/RAM:\n{await cpuRamTask}\nDisk:\n{await diskTask}";
        }
        catch (Exception ex)
        {
            return $"Failed to get system health: {ex.Message}";
        }
    }

    public async Task<string> ReadLogSummaryAsync(string logFilePath, int lines = 100, CancellationToken cancellationToken = default)
    {
        lines = Math.Clamp(lines, 1, 1000);
        logger.LogInformation("Toolbox reading log file: {FilePath}", logFilePath);

        if (!IsLogPathAllowed(logFilePath))
        {
            logger.LogWarning("Blocked unauthorized log path access: {Path}", logFilePath);
            return "Error: Log path is not permitted.";
        }

        try
        {
            var command = $"tail -n {lines} '{logFilePath}' | grep -iE 'error|warn|fail|exception' | tail -n 20";
            var result = await shellService.ExecuteCommandAsync(command, cancellationToken: cancellationToken);
            
            if (string.IsNullOrWhiteSpace(result))
            {
                return $"No recent issues found in {logFilePath}.";
            }
            
            return $"==== Log Issues from {logFilePath} ====\n{result}";
        }
        catch (Exception ex)
        {
            return $"Failed to read log summary: {ex.Message}";
        }
    }

    private bool IsLogPathAllowed(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            return AllowedLogDirectories.Any(dir => fullPath.StartsWith(dir, StringComparison.OrdinalIgnoreCase))
                   && !fullPath.Contains("..");
        }
        catch
        {
            return false;
        }
    }
}
