using System.Net.Http.Headers;
using System.Text.Json;

namespace SV22T1080034.Shop.Services
{
    public class UnsplashImageProvider : IImageProvider
    {
        private readonly string? _accessKey;
        private readonly IHttpClientFactory _httpClientFactory;
        private bool? _isAvailable;

        public string Name => "Unsplash";

        public UnsplashImageProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _accessKey = configuration["Unsplash:AccessKey"];
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (_isAvailable.HasValue)
                return _isAvailable.Value;

            if (string.IsNullOrWhiteSpace(_accessKey))
            {
                _isAvailable = false;
                return false;
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _accessKey);
                var response = await client.GetAsync("https://api.unsplash.com/me");
                _isAvailable = response.IsSuccessStatusCode;
            }
            catch
            {
                _isAvailable = false;
            }

            return _isAvailable.Value;
        }

        public async Task<string?> FetchImageUrlAsync(string productName, int productId)
        {
            if (string.IsNullOrWhiteSpace(_accessKey))
                return null;

            try
            {
                using var client = _httpClientFactory.CreateClient();
                var query = Uri.EscapeDataString(productName);
                var url = $"https://api.unsplash.com/search/photos?query={query}&per_page=1&orientation=landscape";

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Client-ID", _accessKey);

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{Name}] API error: {response.StatusCode}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("results", out var results) || results.GetArrayLength() == 0)
                {
                    Console.WriteLine($"[{Name}] No results for: {productName}");
                    return null;
                }

                var firstPhoto = results[0];
                var imageUrls = firstPhoto.GetProperty("urls");
                var regularUrl = imageUrls.GetProperty("regular").GetString();

                return regularUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Error fetching for {productName}: {ex.Message}");
                return null;
            }
        }
    }
}
