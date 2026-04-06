using System.Net.Http.Headers;
using System.Text.Json;
using SV22T1080034.Shop.Services;

namespace SV22T1080034.Shop.Services
{
    public interface IImageFetchService
    {
        Task<string?> FetchAndSaveImageAsync(string productName, int productId);
        Task<Dictionary<string, string>> BulkFetchAndSaveImagesAsync(IEnumerable<(int ProductId, string ProductName)> products);
    }

    public class ImageFetchService : IImageFetchService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IWebHostEnvironment _env;
        private readonly IEnumerable<IImageProvider> _imageProviders;
        private readonly string _imagesPath;

        public ImageFetchService(
            IHttpClientFactory httpClientFactory,
            IWebHostEnvironment env,
            IEnumerable<IImageProvider> imageProviders)
        {
            _httpClientFactory = httpClientFactory;
            _env = env;
            _imageProviders = imageProviders ?? throw new ArgumentNullException(nameof(imageProviders));
            _imagesPath = Path.Combine(env.WebRootPath, "images", "products");

            Console.WriteLine($"[ImageFetch] Khởi tạo ImageFetchService với {_imageProviders.Count()} providers:");
            foreach (var p in _imageProviders)
            {
                Console.WriteLine($"[ImageFetch]   - {p.Name}");
            }
        }

        public async Task<string?> FetchAndSaveImageAsync(string productName, int productId)
        {
            try
            {
                Console.WriteLine($"[ImageFetch] ============ BẮT ĐẦU FETCH ============");
                Console.WriteLine($"[ImageFetch] Product: {productName} (ID: {productId})");
                Console.WriteLine($"[ImageFetch] Có {_imageProviders.Count()} providers:");

                // Kiểm tra từng provider trước
                foreach (var provider in _imageProviders)
                {
                    var isAvail = await provider.IsAvailableAsync();
                    Console.WriteLine($"[ImageFetch] - {provider.Name}: {(isAvail ? "✅ OK" : "❌ KHÔNG KHẢ DỤNG")}");
                }

                // Thử lần lượt các providers
                int providerIndex = 0;
                foreach (var provider in _imageProviders)
                {
                    providerIndex++;
                    try
                    {
                        if (!await provider.IsAvailableAsync())
                        {
                            Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] Provider '{provider.Name}' không khả dụng, bỏ qua.");
                            continue;
                        }

                        Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] Đang thử provider '{provider.Name}' cho: {productName}");

                        var imageUrl = await provider.FetchImageUrlAsync(productName, productId);
                        if (string.IsNullOrEmpty(imageUrl))
                        {
                            Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] Provider '{provider.Name}' không tìm thấy ảnh cho: {productName}");
                            continue;
                        }

                        Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] 🎯 Tìm thấy URL từ {provider.Name}: {imageUrl}");

                        var client = _httpClientFactory.CreateClient();
                        client.Timeout = TimeSpan.FromSeconds(30); // 30s timeout

                        Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] Đang tải ảnh từ URL...");
                        var imageResponse = await client.GetAsync(imageUrl);
                        if (!imageResponse.IsSuccessStatusCode)
                        {
                            Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] ❌ Không thể tải ảnh từ {provider.Name}: HTTP {(int)imageResponse.StatusCode} {imageResponse.StatusCode}");
                            continue;
                        }

                        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();
                        if (imageBytes == null || imageBytes.Length == 0)
                        {
                            Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] ❌ Ảnh rỗng từ {provider.Name}");
                            continue;
                        }

                        Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] ✅ Tải ảnh thành công: {imageBytes.Length} bytes");

                        // Tạo tên file an toàn
                        var slug = System.Text.RegularExpressions.Regex
                            .Replace(productName.ToLower(), @"[^a-z0-9]+", "-")
                            .Trim('-');
                        var extension = GetExtensionFromUrl(imageUrl) ?? ".jpg";
                        var fileName = $"{slug}-{productId}{extension}";

                        // Đảm bảo thư mục tồn tại
                        if (!Directory.Exists(_imagesPath))
                        {
                            Console.WriteLine($"[ImageFetch] Tạo thư mục: {_imagesPath}");
                            Directory.CreateDirectory(_imagesPath);
                        }

                        var filePath = Path.Combine(_imagesPath, fileName);

                        // Lưu file
                        await File.WriteAllBytesAsync(filePath, imageBytes);

                        // Kiểm tra file đã lưu thành công không
                        if (File.Exists(filePath))
                        {
                            var fileInfo = new FileInfo(filePath);
                            Console.WriteLine($"[ImageFetch] ✅✅ ĐÃ LƯU THÀNH CÔNG: {fileName} ({fileInfo.Length} bytes)");
                            Console.WriteLine($"[ImageFetch] ============ KẾT THÚC THÀNH CÔNG ============");
                        }
                        else
                        {
                            Console.WriteLine($"[ImageFetch] ❌❌ LƯU FILE THẤT BẠI: {fileName}");
                        }

                        return fileName;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[ImageFetch] [{providerIndex}/{_imageProviders.Count()}] ❌ Lỗi khi dùng provider '{provider.Name}': {ex.GetType().Name}: {ex.Message}");
                        if (ex is HttpRequestException httpEx)
                        {
                            Console.WriteLine($"[ImageFetch]    HTTP Error: {httpEx.Message}");
                        }
                        // Thử provider tiếp theo
                        continue;
                    }
                }

                Console.WriteLine($"[ImageFetch] ❌❌❌ KHÔNG TÌM THẤY ẢNH CHO: {productName}");
                Console.WriteLine($"[ImageFetch] Đã thử {_imageProviders.Count()} providers nhưng không có provider nào trả về ảnh.");
                Console.WriteLine($"[ImageFetch] ============ KẾT THÚC THẤT BẠI ============");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ImageFetch] ❌❌ LỖI TỔNG THỂ: {ex.GetType().Name}: {ex.Message}");
                Console.WriteLine($"[ImageFetch] StackTrace: {ex.StackTrace}");
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
                return string.IsNullOrEmpty(ext) ? null : ext;
            }
            catch
            {
                return null;
            }
        }

        public async Task<Dictionary<string, string>> BulkFetchAndSaveImagesAsync(IEnumerable<(int ProductId, string ProductName)> products)
        {
            var results = new Dictionary<string, string>();
            var tasks = new List<Task>();

            foreach (var (productId, productName) in products)
            {
                tasks.Add(Task.Run(async () =>
                {
                    var fileName = await FetchAndSaveImageAsync(productName, productId);
                    lock (results)
                    {
                        results[productId.ToString()] = fileName ?? "FAILED";
                    }
                }));
            }

            await Task.WhenAll(tasks);
            return results;
        }
    }
}
