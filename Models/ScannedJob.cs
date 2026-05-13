using System.ComponentModel.DataAnnotations;

namespace MyLinuxBot.Models;

public class ScannedJob
{
    [Key]
    public string JobId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime ScannedAt { get; set; } = DateTime.UtcNow;
}
