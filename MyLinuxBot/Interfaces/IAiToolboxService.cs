namespace MyLinuxBot.Interfaces;

public interface IAiToolboxService
{
    Task<string> GetSystemHealthAsync(CancellationToken cancellationToken = default);
    Task<string> ReadLogSummaryAsync(string logFilePath, int lines = 100, CancellationToken cancellationToken = default);
    Task<string> ExecuteSafeCommandAsync(string command, CancellationToken cancellationToken = default);
}
