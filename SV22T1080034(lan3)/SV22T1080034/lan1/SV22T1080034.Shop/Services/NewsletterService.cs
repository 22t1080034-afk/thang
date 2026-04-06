using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace SV22T1080034.Shop.Services
{
    public interface INewsletterService
    {
        Task<bool> SubscribeAsync(string email);
        Task<List<string>> GetSubscribersAsync();
    }

    public class NewsletterService : INewsletterService
    {
        private readonly ILogger<NewsletterService> _logger;
        private readonly string _storagePath;

        public NewsletterService(IWebHostEnvironment env, ILogger<NewsletterService> logger)
        {
            _logger = logger;
            _storagePath = Path.Combine(env.ContentRootPath, "App_Data", "newsletter.json");
            EnsureFileExists();
        }

        private void EnsureFileExists()
        {
            try
            {
                var dir = Path.GetDirectoryName(_storagePath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir!); // ! vì chắc chắn directory path không null

                if (!System.IO.File.Exists(_storagePath))
                    System.IO.File.WriteAllText(_storagePath, "[]");
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to create newsletter storage file");
            }
        }

        public async Task<bool> SubscribeAsync(string email)
        {
            try
            {
                var emails = await GetSubscribersAsync();
                var normalizedEmail = email.Trim().ToLower();

                if (emails.Contains(normalizedEmail))
                {
                    _logger?.LogInformation("Newsletter: {Email} already subscribed", email);
                    return false;
                }

                emails.Add(normalizedEmail);
                var json = JsonSerializer.Serialize(emails, new JsonSerializerOptions { WriteIndented = true });
                await System.IO.File.WriteAllTextAsync(_storagePath, json);
                _logger?.LogInformation("Newsletter: {Email} subscribed successfully", email);
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Newsletter subscription failed for {Email}", email);
                return false;
            }
        }

        public async Task<List<string>> GetSubscribersAsync()
        {
            try
            {
                if (!System.IO.File.Exists(_storagePath))
                    return new List<string>();

                var json = await System.IO.File.ReadAllTextAsync(_storagePath);
                return JsonSerializer.Deserialize<List<string>>(json) ?? new List<string>();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Failed to read newsletter subscribers");
                return new List<string>();
            }
        }
    }
}
