# 🖼️ Multi-Provider Image Fetch Service

## 📋 Tổng quan

ImageFetchService hiện tại hỗ trợ **3 API ảnh miễn phí** với cơ chế fallback tự động:

1. **Unsplash** (Priority 1) - Chất lượng cao, cần API key
2. **LoremFlickr** (Priority 2) - Free, keyword-based
3. **Picsum** (Priority 3) - Free, random nhưng ổn định

---

## 🔄 Fallback Chain

```
Unsplash (có key)
    ↓ (fail/hết quota)
LoremFlickr (free)
    ↓ (fail)
Picsum (free fallback)
    ↓ (fail)
RETURN NULL (không có ảnh)
```

---

## 📦 Các Providers

### 1. UnsplashProvider
- **File**: `UnsplashImageProvider.cs`
- **Auth**: Cần API Key trong `appsettings.json`
- **Limit**: 50 requests/giờ (free tier)
- **Quality**: ⭐⭐⭐⭐⭐ (Original quality)
- **Relevance**: ⭐⭐⭐⭐⭐ (search by keyword)
- **URL format**: `https://api.unsplash.com/search/photos?query={keyword}&per_page=1`

**Config** (appsettings.json):
```json
{
  "Unsplash": {
    "AccessKey": "YOUR_ACCESS_KEY"
  }
}
```

---

### 2. LoremFlickrProvider
- **File**: `LoremFlickrImageProvider.cs`
- **Auth**: Không cần
- **Limit**: Unlimited
- **Quality**: ⭐⭐⭐⭐ (400x300)
- **Relevance**: ⭐⭐⭐⭐ (based on keywords extracted from product name)
- **URL format**: `https://loremflickr.com/400/300/{keyword1,keyword2,keyword3}?lock={productId}`

**Tính năng**:
- Tự động extract keywords từ tên sản phẩm (Vietnamese stop words support)
- Fallback to "product" nếu keywords không hợp lệ
- Lock by productId → ảnh ổn định cho cùng 1 product

---

### 3. PicsumProvider
- **File**: `PicsumImageProvider.cs`
- **Auth**: Không cần
- **Limit**: Unlimited
- **Quality**: ⭐⭐⭐⭐⭐ (High quality)
- **Relevance**: ⭐⭐ (random, không liên quan keyword)
- **URL format**: `https://picsum.photos/seed/{productId}/400/300`

**Tính năng**:
- Dùng productId làm seed → luôn trả về ảnh giống nhau cho cùng 1 product
- Đảm bảo có ảnh luôn (nếu 2 provider trên fail)

---

## ⚙️ Cấu hình

### 1. Unsplash AccessKey (nếu dùng Unsplash)

Trong `SV22T1080034.Shop/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "LiteCommerceDB": "Server=...;Database=...;Trusted_Connection=True;"
  },
  "Unsplash": {
    "AccessKey": "Bl9XklxVe0HyZubumGQ4qG3kXxgSEExYjSNeYGweUxU"
  }
}
```

**Lưu ý**: Nếu không có key, Unsplash provider sẽ tự động skip.

---

### 2. Register Providers (đã có trong Program.cs)

```csharp
// Program.cs
builder.Services.AddScoped<UnsplashImageProvider>();
builder.Services.AddScoped<LoremFlickrImageProvider>();
builder.Services.AddScoped<PicsumImageProvider>();

// ImageFetchService sẽ tự động nhận tất cả qua IEnumerable<IImageProvider>
builder.Services.AddScoped<IImageFetchService, ImageFetchService>();
```

---

## 🧪 Testing

### Test từng provider:

```csharp
// Trong ProductController/AutoFetchImage (đã có)
[HttpGet]
public async Task<IActionResult> AutoFetchImage(int id)
{
    var product = await CatalogDataService.GetProductAsync(id);
    if (product == null) return Json(new { success = false, message = "Product not found" });

    var imageFetchService = HttpContext.RequestServices.GetService(typeof(IImageFetchService)) as IImageFetchService;
    var fileName = await imageFetchService.FetchAndSaveImageAsync(product.ProductName, product.ProductID);

    if (!string.IsNullOrEmpty(fileName))
    {
        product.Photo = fileName;
        await CatalogDataService.UpdateProductAsync(product);
        return Json(new { success = true, message = "Image fetched!", photo = fileName });
    }

    return Json(new { success = false, message = "All providers failed" });
}
```

### Check logs:

Console output sẽ hiển thị:
```
[ImageFetch] Trying Unsplash for product: iPhone 15
[ImageFetch] Successfully fetched from Unsplash: iphone-15-123.jpg
```

Hoặc nếu fail:
```
[ImageFetch] Trying Unsplash for product: ... → Warning
[ImageFetch] Trying LoremFlickr for product: ... → Success
```

---

## 🐛 Troubleshooting

### Vấn đề 1: "Unsplash AccessKey chưa được cấu hình"

**Nguyên nhân**: Không có `Unsplash:AccessKey` trong appsettings.json

**Giải pháp**:
- Thêm key vào appsettings.json, HOẶC
- Bỏ qua, service sẽ tự dùng LoremFlickr/Picsum

---

### Vấn đề 2: Ảnh không tải về được

**Kiểm tra**:
1. Internet connection
2. Firewall/Proxy (có block outgoing HTTPS?)
3. Unsplash API limit (50 req/giờ)
4. Product name có special characters?

**Debug**:
- Enable logging trong Program.cs: `builder.Logging.SetMinimumLevel(LogLevel.Information);`
- Xem console output chi tiết

---

### Vấn đề 3: Ảnh lưu sai tên file

**Nguyên nhân**: Product name có ký tự đặc biệt

**Giải pháp**: Code đã dùng regex để slugify:
```csharp
var slug = Regex.Replace(productName.ToLower(), @"[^a-z0-9]+", "-").Trim('-');
```

Nếu vẫn lỗi, kiểm tra productName quá dài (> 100 ký tự).

---

## 📊 Performance

- **Unsplash**: ~500ms-1s (search + download)
- **LoremFlickr**: ~200-500ms
- **Picsum**: ~100-300ms

**Total time**: ~1-2s (nếu Unsplash working), ~2-4s (fallback to Picsum).

---

## 🔄 Implementasi Đã Thay Đổi

### Before:
```csharp
public class ImageFetchService : IImageFetchService
{
    // Chỉ dùng Unsplash
    private readonly string _unsplashAccessKey;
    // ...
}
```

### After:
```csharp
public class ImageFetchService : IImageFetchService
{
    // Constructor nhận tất cả providers
    public ImageFetchService(
        IHttpClientFactory httpClientFactory,
        IWebHostEnvironment env,
        IConfiguration configuration,
        ILogger<ImageFetchService> logger,
        IEnumerable<IImageProvider> providers) // ← injection
    {
        _providers = OrderProviders(providers); // Sort theo priority
    }
}
```

---

## 🎯 Best Practices

1. **Logging**: Tất cả providers đều log success/failure
2. **Graceful degradation**: Nếu Unsplash fail, tự động fallback
3. **Caching**: Lưu ảnh vào local → không fetch lại
4. **Error isolation**: Mỗi provider fail không ảnh hưởng provider khác
5. **Thread-safe**: Mỗi request là independent

---

## 📈 Scalability

- **Nhiều providers**: Chỉ cần thêm class mới implement `IImageProvider`
- **Parallel fetch**: Có thể chạy song song (nhưng hiện tại sequential để tiết kiệm bandwidth)
- **CDN**: Sau này có thể upload ảnh lên S3/Cloudinary

---

## 🔐 Security

- Không expose API keys trong logs
- Validate image size before save (max 5MB)
- Sanitize filename (slugify)

---

## 🧹 Maintenance

### Thêm provider mới:
1. Tạo class implement `IImageProvider`
2. Register trong `Program.cs` với `AddScoped<YourProvider>()`
3. Thêm vào `OrderProviders()` method (nếu cần custom order)

### Xóa provider:
1. Xóa class (hoặc comment registration)
2. Không cần sửa ImageFetchService (vì dùng IEnumerable)

---

## 📝 Example Output

**Success (Unsplash)**:
```
[ImageFetch] Trying Unsplash for product: Samsung Galaxy S24
[ImageFetch] Saved: samsung-galaxy-s24-42.jpg
✅ Successfully fetched from Unsplash: samsung-galaxy-s24-42.jpg
```

**Fallback (Unsplash fail → LoremFlickr success)**:
```
[ImageFetch] Trying Unsplash for product: ... → Warning: rate limit
[ImageFetch] Trying LoremFlickr for product: ...
✅ Successfully fetched from LoremFlickr: laptop-15-87.jpg
```

**All fail**:
```
[ImageFetch] Trying Unsplash for product: ... → Warning
[ImageFetch] Trying LoremFlickr for product: ... → Warning
[ImageFetch] Trying Picsum for product: ... → Warning
⚠️ All image providers failed for product: ...
```

---

**Last Updated**: 2025-04-03
**Version**: 2.0
**Author**: Claude Code
