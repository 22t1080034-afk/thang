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
            // Picsum luôn available
            return true;
        }

        public async Task<string?> FetchImageUrlAsync(string productName, int productId)
        {
            try
            {
                // Dùng productId làm seed để luôn trả về ảnh giống nhau cho cùng 1 product
                var width = 400;
                var height = 300;
                var url = $"https://picsum.photos/seed/{productId}/{width}/{height}";

                // Kiểm tra ảnh tồn tại
                using var client = _httpClientFactory.CreateClient();
                var response = await client.SendAsync(new HttpRequestMessage(HttpMethod.Head, url));

                return response.IsSuccessStatusCode ? url : null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{Name}] Error fetching for {productName}: {ex.Message}");
                return null;
            }
        }
    }
}
