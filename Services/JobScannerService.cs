using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using MyLinuxBot.Interfaces;
using MyLinuxBot.Data;
using MyLinuxBot.Models;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot;

namespace MyLinuxBot.Services;

public class JobScannerService(
    IDbContextFactory<BotDbContext> dbContextFactory,
    ITelegramBotClient botClient,
    IConfiguration config,
    ILogger<JobScannerService> logger) : IJobScannerService
{
    private readonly long _allowedChatId = config.GetValue<long>("ALLOWED_CHAT_ID");

    public async Task<int> ScanAndNotifyAsync(CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Job Scanner multi-site started (Minimalist)...");
        int totalNewJobs = 0;

        var options = new ChromeOptions();
        options.AddArgument("--headless");
        options.AddArgument("--no-sandbox");
        options.AddArgument("--disable-dev-shm-usage");
        options.AddArgument("--disable-gpu");
        options.AddArgument("user-agent=Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/119.0.0.0 Safari/537.36");

        using var driver = new ChromeDriver(options);
        
        var sources = new List<(string Name, string Url, string CardSelector, string TitleSelector, string CompanySelector)>
        {
            ("ITviec", "https://itviec.com/it-jobs/dotnet/ho-chi-minh-hcm", ".job_item", "h3, .title", ".company-name, .name"),
            ("TopCV", "https://www.topcv.vn/tim-viec-lam-dotnet-tai-ho-chi-minh-l2", ".job-item-search-result, .job-item", ".title, h3", ".company, .company-name"),
            ("VietnamWorks", "https://www.vietnamworks.com/viec-lam-dotnet-tai-ho-chi-minh-v29-en", "[class*='job-item'], .job-item", "[class*='job-title'], .title", "[class*='company-name'], .company")
        };

        foreach (var source in sources)
        {
            try
            {
                driver.Navigate().GoToUrl(source.Url);
                await Task.Delay(6000, cancellationToken); // Tăng thời gian đợi để trang load hết

                var jobElements = driver.FindElements(By.CssSelector(source.CardSelector));
                if (jobElements.Count == 0) continue;

                using var dbContext = await dbContextFactory.CreateDbContextAsync(cancellationToken);

                foreach (var element in jobElements.Take(12)) // Tăng số lượng tin kiểm tra
                {
                    try
                    {
                        var title = element.FindElement(By.CssSelector(source.TitleSelector)).Text.Trim();
                        var company = element.FindElement(By.CssSelector(source.CompanySelector)).Text.Trim();
                        var url = element.FindElement(By.TagName("a")).GetAttribute("href");
                        
                        var jobId = $"{source.Name}_{url.Split('/').Last().Split('?').First()}";

                        if (await dbContext.ScannedJobs.AnyAsync(j => j.JobId == jobId, cancellationToken))
                            continue;

                        // Bộ lọc nới lỏng: Lấy tất cả job liên quan đến .NET hoặc Backend
                        if (IsTargetJob(title))
                        {
                            await botClient.SendMessage(_allowedChatId, 
                                $"[{source.Name}] New Job\n\nTitle: {title}\nCompany: {company}\nLink: {url}", 
                                cancellationToken: cancellationToken);

                            dbContext.ScannedJobs.Add(new ScannedJob { JobId = jobId, Title = title, Company = company, Url = url });
                            totalNewJobs++;
                        }
                    }
                    catch { }
                }
                await dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Failed to scan {source.Name}: {ex.Message}");
            }
        }

        driver.Quit();
        return totalNewJobs;
    }

    private bool IsTargetJob(string title)
    {
        // Nới lỏng tối đa: Chỉ cần có .NET, Backend, C#, hoặc Web
        string[] keywords = { ".NET", "Backend", "C#", "Web", "Intern", "Junior", "Fresher", "Thực tập" };
        return keywords.Any(k => title.Contains(k, StringComparison.OrdinalIgnoreCase));
    }
}
