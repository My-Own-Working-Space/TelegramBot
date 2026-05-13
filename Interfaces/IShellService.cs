namespace MyLinuxBot.Interfaces;

public interface IShellService
{
    Task<string> ExecuteCommandAsync(string command, string? workingDirectory = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default);
}
