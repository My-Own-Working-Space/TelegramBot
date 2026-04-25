using CliWrap;
using CliWrap.Buffered;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class ShellService(ILogger<ShellService> logger) : IShellService
{
    private static readonly string[] DangerousCommands = {
        "rm -rf", "mkfs", "reboot", "shutdown", "iptables -F", ":(){ :|:& };:"
    };

    public async Task<string> ExecuteCommandAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        // Security Guard: Check against blacklist
        if (DangerousCommands.Any(cmd => command.Contains(cmd, StringComparison.OrdinalIgnoreCase)))
        {
            logger.LogWarning("Security Guard blocked execution of potentially dangerous command: {Command}", command);
            return $"Error: The command '{command}' is blocked by the Security Guard due to security risks.";
        }

        try
        {
             var cmd = Cli.Wrap("bash")
                 .WithArguments(new[] { "-c", command })
                 .WithValidation(CommandResultValidation.None);

             if (!string.IsNullOrWhiteSpace(workingDirectory))
             {
                 cmd = cmd.WithWorkingDirectory(workingDirectory);
             }

             using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromSeconds(30));
             using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

             var result = await cmd.ExecuteBufferedAsync(linkedCts.Token);
             
             if (result.ExitCode != 0)
             {
                 logger.LogWarning("Command resulted in error. Exit code: {ExitCode}, Error: {Error}", result.ExitCode, result.StandardError);
                 return $"Error: {result.StandardError}\nOutput: {result.StandardOutput}";
             }

             return result.StandardOutput;
        }
        catch (OperationCanceledException)
        {
             return "Command timed out or was cancelled.";
        }
        catch (Exception ex)
        {
             logger.LogError(ex, "Failed to execute command: {Command}", command);
             return $"Exception: {ex.Message}";
        }
    }
}
