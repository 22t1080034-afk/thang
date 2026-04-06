using System.Net.Http.Headers;
using System.Text.Json;

namespace SV22T1080034.Shop.Services
{
    public interface IImageFetchService
    {
        Task<string?> FetchAndSaveImageAsync(string productName, int productId);
    }

    public class ImageFetchService : IImageFetchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IEnumerable<IImageProvider> _providers;
        private readonly string _imagesPath;
        private readonly ILogger<ImageFetchService>? _logger;

        public ImageFetchService(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            IConfiguration configuration,
            ILogger<ImageFetchService> logger,
            IEnumerable<IImageProvider> providers)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _logger = logger;
            _providers = OrderProviders(providers);
            _imagesPath = Path.Combine(env.WebRootPath, "images", "products");
        }

        private List<IImageProvider> OrderProviders(IEnumerable<IImageProvider> providers)
        {
            return providers
                .Where(p => p != null)
                .OrderBy(p => p switch
                {
                    UnsplashImageProvider => 1,
                    LoremFlickrImageProvider => 2,
                    PicsumImageProvider => 3,
                    _ => 99
                })
                .ToList();
        }

        public async Task<string?> FetchAndSaveImageAsync(string productName, int productId)
        {
            if (!_providers.Any())
            {
                _logger?.LogWarning("No image providers configured");
                return null;
            }

            foreach (var provider in _providers)
            {
                try
                {
                    _logger?.LogInformation("Trying {Provider} for product: {ProductName}", provider.Name, productName);

                    var isAvailable = await provider.IsAvailableAsync();
                    if (!isAvailable)
                    {
                        _logger?.LogWarning("{Provider} is not available, skipping...", provider.Name);
                        continue;
                    }

                    var imageUrl = await provider.FetchImageUrlAsync(productName, productId);
                    if (string.IsNullOrEmpty(imageUrl))
                    {
                        _logger?.LogWarning("{Provider} returned no image for {ProductName}", provider.Name, productName);
                        continue;
                    }

                    var fileName = await DownloadAndSaveImageAsync(imageUrl, productName, productId);
                    if (fileName != null)
                    {
                        _logger?.LogInformation("✅ Successfully fetched from {Provider}: {FileName}", provider.Name, fileName);
                        return fileName;
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "❌ Error with {Provider} for {ProductName}", provider.Name, productName);
                }
            }

            _logger?.LogWarning("All image providers failed for {ProductName}", productName);
            return null;
        }

        private async Task<string?> DownloadAndSaveImageAsync(string imageUrl, string productName, int productId)
        {
            try
            {
                using var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync(imageUrl);
                if (!response.IsSuccessStatusCode)
                {
                    _logger?.LogWarning("Failed to download image from {Url}: {StatusCode}", imageUrl, response.StatusCode);
                    return null;
                }

                var imageBytes = await response.Content.ReadAsByteArrayAsync();

                // Create safe filename
                var slug = System.Text.RegularExpressions.Regex
                    .Replace(productName.ToLower(), @"[^a-z0-9]+", "-")
                    .Trim('-');
                var extension = GetExtensionFromUrl(imageUrl) ?? "jpg";
                var fileName = $"{slug}-{productId}.{extension}";

                // Ensure directory exists
                if (!Directory.Exists(_imagesPath))
                {
                    Directory.CreateDirectory(_imagesPath);
                }

                var filePath = Path.Combine(_imagesPath, fileName);

                // Save file
                await File.WriteAllBytesAsync(filePath, imageBytes);

                Console.WriteLine($"[ImageFetch] Saved: {fileName}");

                return fileName;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error downloading image from {Url}", imageUrl);
                return null;
            }
        }

        private string? GetExtensionFromUrl(string url)
        {
            try
            {
                var uri = new Uri(url);
                var path = uri.AbsolutePath;
                var ext = Path.GetExtension(path);
                return string.IsNullOrEmpty(ext) ? null : ext.TrimStart('.');
            }
            catch
            {
                return null;
            }
        }
    }
}
