using System.Net.Http.Headers;
using System.Text.Json;

namespace SV22T1080034.Shop.Services
{
    public class PexelsImageProvider : IImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly string? _apiKey;
        private bool? _isAvailable;

        public string Name => "Pexels";

        public PexelsImageProvider(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _apiKey = configuration["Pexels:ApiKey"];
        }

        public async Task<bool> IsAvailableAsync()
        {
            if (_isAvailable.HasValue)
                return _isAvailable.Value;

            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine($"[{Name}] API Key chưa được cấu hình.");
                _isAvailable = false;
                return false;
            }

            try
            {
                using var client = _httpClientFactory.CreateClient();
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
                var response = await client.GetAsync("https://api.pexels.com/v1/search?query=test&per_page=1");
                _isAvailable = response.IsSuccessStatusCode;
                if (!_isAvailable.Value)
                {
                    Console.WriteLine($"[{Name}] API check failed: {(int)response.StatusCode}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Exception during availability check: {ex.Message}");
                _isAvailable = false;
            }

            return _isAvailable.Value;
        }

        public async Task<string?> FetchImageUrlAsync(string productName, int productId)
        {
            if (string.IsNullOrWhiteSpace(_apiKey))
            {
                Console.WriteLine($"[{Name}] API Key không tồn tại, bỏ qua.");
                return null;
            }

            try
            {
                // Extract keywords từ product name
                var keywords = ExtractKeywords(productName);
                var query = string.Join(" ", keywords.Take(3));

                if (string.IsNullOrEmpty(query))
                {
                    query = "product";
                }

                Console.WriteLine($"[{Name}] Searching for: '{query}' (Product ID: {productId})");

                var url = $"https://api.pexels.com/v1/search?query={Uri.EscapeDataString(query)}&per_page=1&orientation=landscape";

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(15);
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

                var response = await client.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{Name}] API error: {(int)response.StatusCode} - {response.ReasonPhrase}");
                    return null;
                }

                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("photos", out var photos) || photos.GetArrayLength() == 0)
                {
                    Console.WriteLine($"[{Name}] Không tìm thấy ảnh cho: '{query}'");
                    return null;
                }

                var firstPhoto = photos[0];
                var src = firstPhoto.GetProperty("src");
                var originalUrl = src.GetProperty("original").GetString();

                if (string.IsNullOrEmpty(originalUrl))
                {
                    Console.WriteLine($"[{Name}] Không có URL ảnh hợp lệ.");
                    return null;
                }

                Console.WriteLine($"[{Name}] ✅ Found image: {originalUrl}");
                return originalUrl;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Error fetching for {productName}: {ex.GetType().Name}: {ex.Message}");
                return null;
            }
        }

        private List<string> ExtractKeywords(string productName)
        {
            var stopWords = new HashSet<string>
            {
                "va", "cua", "cho", "la", "voi", "nhieu", "cac", "cung", "nay", "kia",
                "day", "the", "moi", "cu", "toi", "ban", "chung", "no", "hoc", "dang",
                "rat", "tot", "dep", "xau", "toi", "u", "co", "khong", "the", "nao",
                "cac", "loai", "san", "pham", "gia", "re", "chinh", "hang"
            };

            var words = productName.ToLower()
                .Split(new char[] { ' ', '-', '_', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .ToList();

            return words;
        }
    }
}
