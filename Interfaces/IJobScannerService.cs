namespace MyLinuxBot.Interfaces;

public interface IJobScannerService
{
    Task<int> ScanAndNotifyAsync(CancellationToken cancellationToken = default);
}
