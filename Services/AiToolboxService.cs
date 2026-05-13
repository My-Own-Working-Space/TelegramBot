using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;
using System.Text.Json;

namespace MyLinuxBot.Services;

public class AiToolboxService : IAiToolboxService
{
    private readonly IShellService _shellService;
    private readonly ILogger<AiToolboxService> _logger;
    private readonly SecurityConfig _securityConfig;

    public AiToolboxService(IShellService shellService, ILogger<AiToolboxService> logger)
    {
        _shellService = shellService;
        _logger = logger;
        
        try 
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "security_config.json");
            if (!File.Exists(configPath)) configPath = "security_config.json";
            
            var json = File.ReadAllText(configPath);
            _securityConfig = JsonSerializer.Deserialize<SecurityConfig>(json) ?? new SecurityConfig();
        }
        catch 
        {
            _securityConfig = new SecurityConfig();
        }
    }

    public async Task<string> ExecuteSafeCommandAsync(string command, CancellationToken ct)
    {
        return await _shellService.ExecuteCommandAsync(command, cancellationToken: ct);
    }

    public async Task<string> GetSystemHealthAsync(CancellationToken ct)
    {
        var df = await _shellService.ExecuteCommandAsync("df -h /", cancellationToken: ct);
        var free = await _shellService.ExecuteCommandAsync("free -h", cancellationToken: ct);
        var uptime = await _shellService.ExecuteCommandAsync("uptime -p", cancellationToken: ct);
        
        return $"System Health:\n\nDisk:\n{df}\n\nMemory:\n{free}\n\nUptime: {uptime}";
    }

    public async Task<string> ReadLogSummaryAsync(string path, int lines, CancellationToken ct)
    {
        if (lines < 1) lines = 10;
        if (lines > 1000) lines = 1000;

        try 
        {
            var fullPath = Path.GetFullPath(path);
            
            bool isAllowed = _securityConfig.AllowedReadDirectories.Any(dir => 
                fullPath.StartsWith(Path.GetFullPath(dir), StringComparison.OrdinalIgnoreCase));

            if (!isAllowed)
            {
                _logger.LogWarning("SEC-LOG-01: Blocked unauthorized log access to {Path}", path);
                return $"Security Error: Access to directory '{Path.GetDirectoryName(fullPath)}' is not allowed.";
            }

            return await _shellService.ExecuteCommandAsync($"tail -n {lines} {fullPath}", cancellationToken: ct);
        }
        catch (Exception ex)
        {
            return $"Error: {ex.Message}";
        }
    }

    public async Task<string> ControlServiceAsync(string serviceName, string action, CancellationToken ct)
    {
        if (!_securityConfig.ServiceWhitelist.Contains(serviceName.ToLower()))
        {
            return $"Security Error: Service '{serviceName}' is not in the whitelist.";
        }

        string[] allowedActions = { "status", "start", "stop", "restart" };
        if (!allowedActions.Contains(action.ToLower()))
        {
            return $"Error: Action '{action}' is not supported.";
        }

        return await _shellService.ExecuteCommandAsync($"systemctl {action} {serviceName}", cancellationToken: ct);
    }
}
