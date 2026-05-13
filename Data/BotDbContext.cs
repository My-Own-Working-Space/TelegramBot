using Microsoft.EntityFrameworkCore;
using MyLinuxBot.Models;

namespace MyLinuxBot.Data;

public class BotDbContext(DbContextOptions<BotDbContext> options) : DbContext(options)
{
    public DbSet<ChatMessage> ChatMessages { get; set; }
    public DbSet<StoryState> StoryStates { get; set; }
    public DbSet<ScannedJob> ScannedJobs { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ChatMessage>().HasIndex(x => x.ChatId);
        modelBuilder.Entity<StoryState>().HasIndex(x => x.ChatId).IsUnique();
    }
}
