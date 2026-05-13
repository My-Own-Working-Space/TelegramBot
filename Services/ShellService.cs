using CliWrap;
using CliWrap.Buffered;
using System.Text.RegularExpressions;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Models;
using System.Text.Json;

namespace MyLinuxBot.Services;

public class ShellService : IShellService
{
    private readonly ILogger<ShellService> _logger;
    private readonly SecurityConfig _securityConfig;
    private static readonly Regex UnsafeCharsRegex = new(@"[|&;$\(\)> <\n\r\t`]", RegexOptions.Compiled);

    public ShellService(ILogger<ShellService> logger)
    {
        _logger = logger;
        try 
        {
            var configPath = Path.Combine(AppContext.BaseDirectory, "security_config.json");
            if (!File.Exists(configPath)) configPath = "security_config.json";
            
            var json = File.ReadAllText(configPath);
            _securityConfig = JsonSerializer.Deserialize<SecurityConfig>(json) ?? new SecurityConfig();
        }
        catch (Exception ex)
        {
            _logger.LogCritical(ex, "Failed to load security_config.json.");
            _securityConfig = new SecurityConfig();
        }
    }

    public async Task<string> ExecuteCommandAsync(string fullCommand, string? workingDirectory = null, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fullCommand)) return "Command is empty.";

        var parts = fullCommand.Trim().Split(' ', 2);
        var binary = parts[0];
        var args = parts.Length > 1 ? parts[1] : "";

        // SEC-CMD-01: Whitelist check
        var rule = _securityConfig.CommandWhitelist.FirstOrDefault(r => r.Binary.Equals(binary, StringComparison.OrdinalIgnoreCase));
        
        if (rule == null)
        {
            _logger.LogWarning("SEC-LOG-01: Blocked unauthorized binary: {Binary}", binary);
            return $"Security Error: Binary '{binary}' is not in the whitelist.";
        }

        // Validate arguments against regex
        if (!string.IsNullOrEmpty(rule.AllowedArgs) && !Regex.IsMatch(args, rule.AllowedArgs))
        {
            _logger.LogWarning("SEC-LOG-01: Blocked unauthorized arguments for {Binary}: {Args}", binary, args);
            return $"Security Error: Arguments do not match the allowed pattern for '{binary}'.";
        }

        try
        {
            _logger.LogInformation("Executing secure command: {Binary} {Args}", binary, args);

            // SEC-CMD-04: Timeout logic
            var effectiveTimeout = timeout ?? TimeSpan.FromSeconds(_securityConfig.DefaultTimeoutSeconds);
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(effectiveTimeout);

            var cmd = Cli.Wrap(binary).WithArguments(args).WithValidation(CommandResultValidation.None);
            
            if (!string.IsNullOrEmpty(workingDirectory))
            {
                cmd = cmd.WithWorkingDirectory(workingDirectory);
            }

            var result = await cmd.ExecuteBufferedAsync(cts.Token);
            var output = result.StandardOutput + result.StandardError;

            // SEC-CMD-03: Limit output to 64KB
            if (output.Length > _securityConfig.MaxOutputSize)
            {
                output = output[.._securityConfig.MaxOutputSize] + "\n[Output truncated at 64KB]";
            }

            return string.IsNullOrWhiteSpace(output) ? "(no output)" : output;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning("SEC-LOG-01: Command timed out: {Binary}", binary);
            return $"Execution Error: Command timed out after {timeout?.TotalSeconds ?? _securityConfig.DefaultTimeoutSeconds}s.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Execution Error for {Binary}", binary);
            return $"Execution Error: {ex.Message}";
        }
    }
}
