using CliWrap;
using CliWrap.Buffered;
using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Services;

public class ShellService(ILogger<ShellService> logger) : IShellService
{
    public async Task<string> ExecuteCommandAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        try
        {
             var cmd = Cli.Wrap("bash")
                 .WithArguments(new[] { "-c", command })
                 .WithValidation(CommandResultValidation.None);

             if (!string.IsNullOrWhiteSpace(workingDirectory))
             {
                 cmd = cmd.WithWorkingDirectory(workingDirectory);
             }

             using var cts = new CancellationTokenSource(timeout ?? TimeSpan.FromMinutes(2));
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
