namespace MyLinuxBot.Models;

public class ChatMessage
{
    public int Id { get; set; }
    public long ChatId { get; set; }
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
