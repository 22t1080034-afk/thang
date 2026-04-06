using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.Shop.Services;

namespace SV22T1080034.Shop.Controllers
{
    public class ProductController : Controller
    {
        public ProductController()
        {
        }

        [HttpGet]
        public async Task<IActionResult> AutoFetchImage(int id)
        {
            try
            {
                var product = await CatalogDataService.GetProductAsync(id);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                if (!string.IsNullOrEmpty(product.Photo))
                {
                    return Json(new { success = true, message = "Sản phẩm đã có ảnh.", photo = product.Photo });
                }

                // Lấy service từ HttpContext
                var imageFetchService = HttpContext.RequestServices.GetService(typeof(IImageFetchService)) as IImageFetchService;
                if (imageFetchService == null)
                {
                    return Json(new { success = false, message = "ImageFetchService chưa được cấu hình." });
                }

                var fileName = await imageFetchService.FetchAndSaveImageAsync(product.ProductName, product.ProductID);
                if (!string.IsNullOrEmpty(fileName))
                {
                    product.Photo = fileName;
                    await CatalogDataService.UpdateProductAsync(product);

                    return Json(new { success = true, message = "Đã tải ảnh thành công!", photo = fileName });
                }
                else
                {
                    return Json(new { success = false, message = "Không tìm thấy ảnh phù hợp." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[AutoFetchImage Error] {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải ảnh. Vui lòng thử lại sau." });
            }
        }

        [HttpGet]
        public async Task<IActionResult> BulkFetchImages(int limit = 50)
        {
            try
            {
                // Lấy danh sách sản phẩm không có ảnh (giới hạn số lượng để tránh quá tải)
                var input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 1000,
                    SearchValue = ""
                };
                var allProducts = await CatalogDataService.ListProductsAsync(input);

                var productsWithoutImages = allProducts.DataItems
                    .Where(p => string.IsNullOrEmpty(p.Photo))
                    .Take(limit)
                    .Select(p => (p.ProductID, p.ProductName))
                    .ToList();

                if (productsWithoutImages.Count == 0)
                {
                    return Json(new { success = true, message = "Không có sản phẩm nào cần tải ảnh.", total = 0 });
                }

                var imageFetchService = HttpContext.RequestServices.GetService(typeof(IImageFetchService)) as IImageFetchService;
                if (imageFetchService == null)
                {
                    return Json(new { success = false, message = "ImageFetchService chưa được cấu hình." });
                }

                Console.WriteLine($"[BulkFetch] Bắt đầu tải ảnh cho {productsWithoutImages.Count} sản phẩm...");

                var results = await imageFetchService.BulkFetchAndSaveImagesAsync(productsWithoutImages);

                // Cập nhật database
                int updatedCount = 0;
                foreach (var product in allProducts.DataItems.Where(p => string.IsNullOrEmpty(p.Photo)))
                {
                    if (results.TryGetValue(product.ProductID.ToString(), out var fileName) && fileName != "FAILED")
                    {
                        product.Photo = fileName;
                        await CatalogDataService.UpdateProductAsync(product);
                        updatedCount++;
                    }
                }

                Console.WriteLine($"[BulkFetch] Hoàn thành: {updatedCount}/{productsWithoutImages.Count} thành công");

                return Json(new
                {
                    success = true,
                    message = $"Đã xử lý {productsWithoutImages.Count} sản phẩm. Thành công: {updatedCount}, Thất bại: {productsWithoutImages.Count - updatedCount}",
                    total = productsWithoutImages.Count,
                    successCount = updatedCount,
                    failedCount = productsWithoutImages.Count - updatedCount,
                    details = results
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[BulkFetch] Lỗi: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[BulkFetch StackTrace] {ex.StackTrace}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi tải ảnh. Vui lòng thử lại sau." });
            }
        }

        // GET: /Shop/Product
        public async Task<IActionResult> Index(string searchValue = "", int categoryID = 0, decimal minPrice = 0, decimal maxPrice = 0, int page = 1, int pageSize = 12)
        {
            var input = new ProductSearchInput()
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue,
                CategoryID = categoryID,
                MinPrice = minPrice > 0 ? minPrice : null,
                MaxPrice = maxPrice > 0 ? maxPrice : null
            };

            var result = await CatalogDataService.ListProductsAsync(input);
            ViewBag.Title = "Danh sách sản phẩm - Thang Shop";
            ViewBag.SearchValue = searchValue;
            ViewBag.CategoryID = categoryID;
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.PageSize = pageSize; // Thêm này để giữ pageSize

            var categoryInput = new PaginationSearchInput() { Page = 1, PageSize = 1000 };
            var categories = await CatalogDataService.ListCategoriesAsync(categoryInput);
            ViewBag.Categories = categories.DataItems;

            return View(result);
        }

        public async Task<IActionResult> Details(int id)
        {
            var product = await CatalogDataService.GetProductAsync(id);
            if (product == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sản phẩm.";
                return RedirectToAction("Index", "Home");
            }

            // Lấy thuộc tính của sản phẩm
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attributes;

            // Lấy ảnh của sản phẩm
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Photos = photos;

            ViewBag.Title = $"{product.ProductName} - Thang Shop";
            return View(product);
        }

        public async Task<IActionResult> Related(int categoryID, int productID, int limit = 4)
        {
            System.Diagnostics.Debug.WriteLine($"[Related] categoryID={categoryID}, productID={productID}, limit={limit}");

            if (categoryID <= 0)
            {
                System.Diagnostics.Debug.WriteLine("[Related] Invalid categoryID, returning empty");
                return Content("");
            }

            var input = new ProductSearchInput()
            {
                Page = 1,
                PageSize = limit + 1, 
                CategoryID = categoryID
            };

            var result = await CatalogDataService.ListProductsAsync(input);
            System.Diagnostics.Debug.WriteLine($"[Related] Found {result?.DataItems?.Count ?? 0} products in category");

            // Loại bỏ sản phẩm hiện tại
            var related = result.DataItems.Where(p => p.ProductID != productID).Take(limit).ToList();
            System.Diagnostics.Debug.WriteLine($"[Related] After filtering current product: {related.Count} related products");

            return PartialView("_RelatedProducts", related);
        }
    }
}
