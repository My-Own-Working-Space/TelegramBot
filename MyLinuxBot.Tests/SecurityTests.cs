using Xunit;
using Microsoft.Extensions.Logging;
using MyLinuxBot.Services;
using MyLinuxBot.Interfaces;
using Moq;

namespace MyLinuxBot.Tests;

public class SecurityTests
{
    private readonly Mock<ILogger<ShellService>> _shellLoggerMock = new();
    private readonly Mock<ILogger<AiToolboxService>> _toolboxLoggerMock = new();

    [Fact]
    public async Task ShellService_ShouldBlock_NonWhitelistedCommand()
    {
        var service = new ShellService(_shellLoggerMock.Object);
        var result = await service.ExecuteCommandAsync("rm -rf /");
        Assert.Contains("Security Error", result);
    }

    [Fact]
    public async Task ShellService_ShouldBlock_InvalidArguments()
    {
        var service = new ShellService(_shellLoggerMock.Object);
        var result = await service.ExecuteCommandAsync("free -m");
        Assert.Contains("Security Error", result);
    }

    [Fact]
    public async Task AiToolboxService_ShouldBlock_PathTraversal()
    {
        var shellMock = new Mock<IShellService>();
        var service = new AiToolboxService(shellMock.Object, _toolboxLoggerMock.Object);
        var result = await service.ReadLogSummaryAsync("/var/log/../../etc/passwd", 10, CancellationToken.None);
        Assert.Contains("Security Error", result);
    }
}
