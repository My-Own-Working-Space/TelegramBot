using DotNetEnv;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;
using Telegram.Bot.Polling;
using MyLinuxBot.Data;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Services;
using MyLinuxBot.Commands;

// Load environment variables from .env file
Env.Load();

var builder = WebApplication.CreateBuilder(args);
builder.Configuration.AddEnvironmentVariables();

var botToken = builder.Configuration["TELEGRAM_BOT_TOKEN"];
if (string.IsNullOrWhiteSpace(botToken) || botToken == "your_bot_token_here")
{
    Console.WriteLine("Warning: TELEGRAM_BOT_TOKEN is missing or not configured. Set it in the .env file.");
}

// 1. Register Database (SQLite)
builder.Services.AddDbContext<BotDbContext>(options =>
{
    // Use a local SQLite database
    options.UseSqlite($"Data Source=MyLinuxBot.db");
});

// 2. Register Telegram Bot Components
var client = new TelegramBotClient(botToken ?? "dummy_token_to_allow_build");
builder.Services.AddSingleton<ITelegramBotClient>(client);
builder.Services.AddSingleton<IUpdateHandler, BotUpdateHandler>();
builder.Services.AddHostedService<BotHostedService>();

// 3. Register Domain Services
builder.Services.AddSingleton<IShellService, ShellService>();
builder.Services.AddSingleton<IAiToolboxService, AiToolboxService>();
builder.Services.AddHttpClient<IGeminiService, GeminiService>();
builder.Services.AddHttpClient<IN8nIntegrationService, N8nIntegrationService>();

// 4. Register Commands (Command Pattern)
builder.Services.AddTransient<ITelegramCommand, ShellCommand>();
builder.Services.AddTransient<ITelegramCommand, K8sCommand>();
builder.Services.AddTransient<ITelegramCommand, AskCommand>();
builder.Services.AddTransient<ITelegramCommand, UploadCommand>();

var app = builder.Build();

// Initialize Database schema
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<BotDbContext>();
    dbContext.Database.EnsureCreated();
}

// 5. Minimal API webhook endpoint integration
app.MapPost("/webhook/n8n", async (ILogger<Program> logger, IConfiguration config, ITelegramBotClient bot) => 
{
    logger.LogInformation("Webhook invoked from n8n.");
    var allowedChatId = config.GetValue<long>("ALLOWED_CHAT_ID");
    
    if (allowedChatId != 0) 
    {
        try
        {
            await bot.SendMessage(allowedChatId, "Alert: Webhook received from n8n!");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send webhook notification.");
        }
    }

    return Results.Ok(new { success = true });
});

// 6. API Endpoint for n8n Agent Actions
app.MapPost("/api/execute", async (HttpContext context, IConfiguration config, IShellService shellService) =>
{
    var apiKey = config["N8N_API_KEY"];
    if (!context.Request.Headers.TryGetValue("X-API-Key", out var extractedApiKey) || extractedApiKey != apiKey)
    {
        return Results.Unauthorized();
    }

    using var streamReader = new StreamReader(context.Request.Body);
    var body = await streamReader.ReadToEndAsync();
    var payload = System.Text.Json.JsonSerializer.Deserialize<ExecutePayload>(body, new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true });

    if (payload == null || string.IsNullOrWhiteSpace(payload.Command))
    {
        return Results.BadRequest(new { error = "Command payload is missing or invalid." });
    }

    string? workingDirectory = payload.Type?.ToLower() == "k8s" 
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents/Documents/K8s/Setup") 
        : null;

    var result = await shellService.ExecuteCommandAsync(payload.Command, workingDirectory);

    return Results.Ok(new { success = true, output = result });
});

app.Run();

public class ExecutePayload { public string? Command { get; set; } public string? Type { get; set; } }
