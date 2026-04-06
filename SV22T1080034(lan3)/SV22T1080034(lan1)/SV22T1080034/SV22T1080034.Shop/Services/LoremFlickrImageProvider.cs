namespace SV22T1080034.Shop.Services
{
    public class LoremFlickrImageProvider : IImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Name => "LoremFlickr";

        public LoremFlickrImageProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // LoremFlickr không hỗ trợ HEAD, dùng GET với timeout ngắn
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var testUrl = "https://loremflickr.com/400/300/test?lock=1";
                var response = await client.GetAsync(testUrl);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string?> FetchImageUrlAsync(string productName, int productId)
        {
            try
            {
                var keywords = ExtractKeywords(productName);
                var query = string.Join(",", keywords.Take(3));

                // Nếu không có keywords, dùng "product"
                if (string.IsNullOrEmpty(query))
                {
                    query = "product";
                }

                var url = $"https://loremflickr.com/400/300/{query}?lock={productId}";

                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);

                // Thử URL chính
                var response = await client.GetAsync(url);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{Name}] ✅ Found image for '{query}': {url}");
                    return url;
                }

                Console.WriteLine($"[{Name}] ❌ Main URL failed: {(int)response.StatusCode}");

                // Try fallback to "product"
                if (query != "product")
                {
                    var fallbackUrl = $"https://loremflickr.com/400/300/product?lock={productId}";
                    var fallbackResponse = await client.GetAsync(fallbackUrl);
                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        Console.WriteLine($"[{Name}] ✅ Fallback to 'product' worked: {fallbackUrl}");
                        return fallbackUrl;
                    }
                    Console.WriteLine($"[{Name}] ❌ Fallback also failed: {(int)fallbackResponse.StatusCode}");
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Error fetching for {productName}: {ex.Message}");
                return null;
            }
        }

        private List<string> ExtractKeywords(string productName)
        {
            var stopWords = new HashSet<string>
            {
                "va", "cua", "cho", "la", "voi", "nhieu", "cac", "cung", "nay", "kia",
                "day", "the", "moi", "cu", "toi", "ban", "chung", "no", "hoc", "dang",
                "rat", "tot", "dep", "xau", "toi", "u", "co", "khong", "the", "nao"
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
