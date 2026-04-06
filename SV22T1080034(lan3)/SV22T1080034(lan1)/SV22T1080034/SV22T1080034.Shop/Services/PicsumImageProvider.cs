namespace SV22T1080034.Shop.Services
{
    public class PicsumImageProvider : IImageProvider
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public string Name => "Picsum";

        public PicsumImageProvider(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                // Picsum không hỗ trợ HEAD, dùng GET nhẹ với timeout ngắn
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var testUrl = "https://picsum.photos/seed/test/1/1";
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
                var width = 400;
                var height = 300;
                var url = $"https://picsum.photos/seed/{productId}/{width}/{height}";

                // Kiểm tra URL có reachable không bằng GET (không cần tải toàn bộ ảnh)
                using var client = _httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(10);
                var request = new HttpRequestMessage(HttpMethod.Get, url);
                request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(0, 0); // Chỉ lấy 1 byte

                var response = await client.SendAsync(request);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"[{Name}] URL not accessible: {(int)response.StatusCode}");
                    return null;
                }

                return url;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Error fetching for {productName}: {ex.Message}");
                return null;
            }
        }
    }
}
