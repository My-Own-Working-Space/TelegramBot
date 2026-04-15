namespace MyLinuxBot.Interfaces;

public interface IBotService
{
    Task StartReceivingAsync(CancellationToken cancellationToken);
}
