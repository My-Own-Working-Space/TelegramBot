namespace MyLinuxBot.Models;

public class SecurityConfig
{
    public List<CommandRule> CommandWhitelist { get; set; } = new();
    public List<string> ServiceWhitelist { get; set; } = new();
    public List<string> AllowedReadDirectories { get; set; } = new();
    public List<string> AllowedWriteDirectories { get; set; } = new();
    public int MaxOutputSize { get; set; } = 65536;
    public int DefaultTimeoutSeconds { get; set; } = 30;
    public int MaxLoops { get; set; } = 3;
}

public class CommandRule
{
    public string Binary { get; set; } = "";
    public string AllowedArgs { get; set; } = "";
    public string Description { get; set; } = "";
}
