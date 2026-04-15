namespace MyLinuxBot.Models;

public class StoryState
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public required string CurrentStep { get; set; }
    public string? Payload { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
