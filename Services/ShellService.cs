using CliWrap;
using CliWrap.Buffered;
using MyLinuxBot.Interfaces;
using System.Text;

namespace MyLinuxBot.Services;

public class ShellService(ILogger<ShellService> logger) : IShellService
{
    private static readonly HashSet<string> AllowedBinaries = new(StringComparer.OrdinalIgnoreCase)
    {
        "df", "free", "top", "ps", "uptime", "cat", "tail", "grep", "ls", 
        "sensors", "scrot", "reboot", "shutdown", "systemctl", "uptime"
    };

    public async Task<string> ExecuteCommandAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var trimmedCommand = command.Trim();
        var binary = trimmedCommand.Split(' ')[0];

        // Audit Log
        logger.LogInformation("Execution request: {Command}", trimmedCommand);

        if (!AllowedBinaries.Contains(binary))
        {
            logger.LogWarning("Blocked unauthorized command: {Binary}", binary);
            return $"Error: Command '{binary}' is not permitted.";
        }

        try
        {
             var cmd = Cli.Wrap("bash")
                 .WithArguments(new[] { "-c", trimmedCommand })
                 .WithValidation(CommandResultValidation.None);

             if (!string.IsNullOrWhiteSpace(workingDirectory))
             {
                 cmd = cmd.WithWorkingDirectory(workingDirectory);
             }

             using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
             using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

             var result = await cmd.ExecuteBufferedAsync(linkedCts.Token);
             
             const int maxOutputBytes = 64 * 1024; // 64KB
             var output = result.StandardOutput;
             if (output.Length > maxOutputBytes)
             {
                 output = output[..maxOutputBytes] + "\n[Output truncated due to size]";
             }

             if (result.ExitCode != 0)
             {
                 logger.LogWarning("Command failed with exit code {ExitCode}: {Error}", result.ExitCode, result.StandardError);
                 return $"Error: {result.StandardError}\nOutput: {output}";
             }

             return output;
        }
        catch (OperationCanceledException)
        {
             logger.LogWarning("Command timed out: {Command}", trimmedCommand);
             return "Command timed out or was cancelled.";
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Failed to execute: {Command}", trimmedCommand);
             return $"Exception: {ex.Message}";
        }
    }
}
