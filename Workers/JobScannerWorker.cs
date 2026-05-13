using MyLinuxBot.Interfaces;

namespace MyLinuxBot.Workers;

public class JobScannerWorker(
    IServiceProvider serviceProvider,
    ILogger<JobScannerWorker> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Job Scanner Worker is starting...");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = serviceProvider.CreateScope();
                var scanner = scope.ServiceProvider.GetRequiredService<IJobScannerService>();
                int count = await scanner.ScanAndNotifyAsync(stoppingToken);
                logger.LogInformation($"Auto-scan completed. Found {count} new jobs.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "An error occurred in Job Scanner Worker.");
            }

            await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
        }
    }
}
