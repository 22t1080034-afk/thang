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
            // LoremFlickr luôn available (không cần auth)
            return true;
        }

        public async Task<string?> FetchImageUrlAsync(string productName, int productId)
        {
            try
            {
                // Extract keyword từ product name (lấy từng từ, bỏ stop words)
                var keywords = ExtractKeywords(productName);
                var query = string.Join(",", keywords.Take(3)); // Lấy tối đa 3 keywords

                // LoremFlickr URL format
                var url = $"https://loremflickr.com/400/300/{query}?lock={productId}";

                // Kiểm tra xem ảnh tồn tại (HEAD request)
                using var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                if (response.IsSuccessStatusCode)
                {
                    return url;
                }

                // Nếu có keyword không tốt, thử với generic "product"
                if (keywords.Any())
                {
                    var fallbackUrl = $"https://loremflickr.com/400/300/product?lock={productId}";
                    var fallbackResponse = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, fallbackUrl));
                    if (fallbackResponse.IsSuccessStatusCode)
                    {
                        return fallbackUrl;
                    }
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
            // Vietnamese stop words (từ không quan trọng)
            var stopWords = new HashSet<string>
            {
                "va", "cua", "cho", "la", "voi", "nhieu", "cac", "cung", "nay", "kia",
                "day", "the", "moi", "cu", "toi", "ban", "chung", "no", "hoc", "dang",
                "rat", "tot", "-dep", "xau", "toi", "u", "co", "khong", "the", "nao"
            };

            // Lowercase và split
            var words = productName.ToLower()
                .Split(new char[] { ' ', '-', '_', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(w => new string(w.Where(char.IsLetterOrDigit).ToArray()))
                .Where(w => w.Length > 2 && !stopWords.Contains(w))
                .ToList();

            return words;
        }
    }
}
