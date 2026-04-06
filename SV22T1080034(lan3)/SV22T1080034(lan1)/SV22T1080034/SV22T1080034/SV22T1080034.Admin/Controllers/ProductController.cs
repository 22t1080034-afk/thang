using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Common;
using System.IO;
using System;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ProductController : Controller
    {
        public const string SEARCH_PRODUCT = "SearchProduct";

        /// <summary>
        /// Trang quản lý mặt hàng
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);

            if (input == null)
            {
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = "",
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = null,
                    MaxPrice = null
                };
            }

            return View(input);
        }

        /// <summary>
        /// Tìm kiếm + phân trang
        /// </summary>
        public async Task<IActionResult> Search(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);

            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);

            return View(result);
        }

        // Product/Create
        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung mặt hàng";

            var model = new Product()
            {
                ProductID = 0,
                IsSelling = true
            };

            return View("Edit", model);
        }

        // Product/Edit/{id} - SỬA: Thêm load Attributes và Photos
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật mặt hàng";

            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Load thuộc tính của sản phẩm
            var attributes = await CatalogDataService.ListAttributesAsync(id);
            ViewBag.Attributes = attributes;

            // Load ảnh của sản phẩm
            var photos = await CatalogDataService.ListPhotosAsync(id);
            ViewBag.Photos = photos;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Product data, IFormFile? uploadPhoto)
        {
            ViewBag.Title = data.ProductID == 0 ? "Bổ sung mặt hàng" : "Cập nhật mặt hàng";

            if (string.IsNullOrWhiteSpace(data.ProductName))
                ModelState.AddModelError(nameof(data.ProductName), "Tên mặt hàng không được bỏ trống");

            if (data.Price <= 0)
                ModelState.AddModelError(nameof(data.Price), "Giá phải > 0");

            // Validate CategoryID và SupplierID
            if (data.CategoryID == null || data.CategoryID <= 0)
                ModelState.AddModelError(nameof(data.CategoryID), "Vui lòng chọn loại hàng");

            if (data.SupplierID == null || data.SupplierID <= 0)
                ModelState.AddModelError(nameof(data.SupplierID), "Vui lòng chọn nhà cung cấp");

            if (!ModelState.IsValid)
                return View("Edit", data);

            // Upload ảnh
            if (uploadPhoto != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                var adminPath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);

                // Lưu ảnh vào Admin wwwroot
                using (var stream = new FileStream(adminPath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                // SYNC: Copy ảnh sang Shop wwwroot để hiển thị trên frontend
                try
                {
                    var shopProjectPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..",
                        "SV22T1080034.Shop"
                    );
                    var shopPath = Path.Combine(shopProjectPath, "wwwroot", "images", "products", fileName);

                    // Tạo thư mục nếu chưa có
                    var shopDir = Path.GetDirectoryName(shopPath);
                    if (!Directory.Exists(shopDir))
                    {
                        Directory.CreateDirectory(shopDir);
                    }

                    // Copy file (overwrite nếu đã có)
                    System.IO.File.Copy(adminPath, shopPath, overwrite: true);
                    Console.WriteLine($"[Admin] Đã copy ảnh sang Shop: {fileName}");
                }
                catch (Exception ex)
                {
                    // Log nhưng không block upload
                    Console.WriteLine($"[Admin] Lỗi khi copy ảnh sang Shop: {ex.Message}");
                }

                data.Photo = fileName;
            }

            if (string.IsNullOrEmpty(data.Photo))
                data.Photo = "nophoto.png";

            if (data.ProductID == 0)
            {
                await CatalogDataService.AddProductAsync(data);
                PaginationSearchInput input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = data.ProductName
                };
                ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            }
            else
                await CatalogDataService.UpdateProductAsync(data);

            TempData["SuccessMessage"] = data.ProductID == 0 ? "Thêm mặt hàng thành công!" : "Cập nhật mặt hàng thành công!";
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        // Product/Delete/{id}
        public async Task<IActionResult> Delete(int id)
        {
            // Nếu là POST → thực hiện xóa
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteProductAsync(id);
                TempData["SuccessMessage"] = "Xóa mặt hàng thành công!";
                return RedirectToAction("Index");
            }

            // Nếu là GET → hiển thị xác nhận xóa
            var model = await CatalogDataService.GetProductAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra có được phép xóa không
            bool allowDelete = !(await CatalogDataService.IsUsedProductAsync(id));
            ViewBag.AllowDelete = allowDelete;
            return View(model);
        }

        // ==================== ATTRIBUTES ====================

        public IActionResult CreateAttribute(int id)
        {
            var model = new ProductAttribute() { ProductID = id, AttributeID = 0 };
            return View("EditAttribute", model);
        }

        public async Task<IActionResult> EditAttribute(int id, long attributeId)
        {
            var model = await CatalogDataService.GetAttributeAsync(attributeId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveAttribute(ProductAttribute data)
        {
            if (string.IsNullOrWhiteSpace(data.AttributeName))
                ModelState.AddModelError(nameof(data.AttributeName), "Vui lòng nhập tên thuộc tính");
            if (string.IsNullOrWhiteSpace(data.AttributeValue))
                ModelState.AddModelError(nameof(data.AttributeValue), "Vui lòng nhập giá trị");

            if (!ModelState.IsValid)
                return View("EditAttribute", data);

            if (data.AttributeID == 0)
                await CatalogDataService.AddAttributeAsync(data);
            else
                await CatalogDataService.UpdateAttributeAsync(data);

            TempData["SuccessMessage"] = "Lưu thuộc tính thành công!";
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        public async Task<IActionResult> DeleteAttribute(int id, long attributeId)
        {
            await CatalogDataService.DeleteAttributeAsync(attributeId);
            TempData["SuccessMessage"] = "Đã xóa thuộc tính!";
            return RedirectToAction("Edit", new { id = id });
        }

        // ==================== PHOTOS ====================

        public IActionResult CreatePhoto(int id)
        {
            var model = new ProductPhoto() { ProductID = id, PhotoID = 0, IsHidden = false };
            return View("EditPhoto", model);
        }

        [HttpPost]
        public async Task<IActionResult> SavePhoto(ProductPhoto data, IFormFile? uploadPhoto)
        {
            if (string.IsNullOrWhiteSpace(data.Description)) data.Description = "";

            // Xử lý upload ảnh
            if (uploadPhoto != null)
            {
                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                var adminPath = Path.Combine(ApplicationContext.WWWRootPath, "images/products", fileName);

                using (var stream = new FileStream(adminPath, FileMode.Create))
                {
                    await uploadPhoto.CopyToAsync(stream);
                }

                // SYNC: Copy ảnh sang Shop wwwroot
                try
                {
                    var shopProjectPath = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory,
                        "..", "..", "..",
                        "SV22T1080034.Shop"
                    );
                    var shopPath = Path.Combine(shopProjectPath, "wwwroot", "images", "products", fileName);

                    var shopDir = Path.GetDirectoryName(shopPath);
                    if (!Directory.Exists(shopDir))
                    {
                        Directory.CreateDirectory(shopDir);
                    }

                    System.IO.File.Copy(adminPath, shopPath, overwrite: true);
                    Console.WriteLine($"[Admin] Đã copy ảnh phụ sang Shop: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Admin] Lỗi copy ảnh phụ: {ex.Message}");
                }

                data.Photo = fileName;
            }

            if (string.IsNullOrEmpty(data.Photo))
                ModelState.AddModelError(nameof(data.Photo), "Vui lòng chọn ảnh");

            if (!ModelState.IsValid) return View("EditPhoto", data);

            if (data.PhotoID == 0)
                await CatalogDataService.AddPhotoAsync(data);
            else
                await CatalogDataService.UpdatePhotoAsync(data);

            TempData["SuccessMessage"] = "Lưu ảnh thành công!";
            return RedirectToAction("Edit", new { id = data.ProductID });
        }

        public async Task<IActionResult> EditPhoto(int id, long photoId)
        {
            var model = await CatalogDataService.GetPhotoAsync(photoId);
            if (model == null) return RedirectToAction("Edit", new { id = id });
            return View(model);
        }

        // SỬA: Thêm thông báo thành công khi xóa ảnh
        public async Task<IActionResult> DeletePhoto(int id, long photoId)
        {
            await CatalogDataService.DeletePhotoAsync(photoId);
            TempData["SuccessMessage"] = "Đã xóa ảnh thành công khỏi thư viện!";
            return RedirectToAction("Edit", new { id = id });
        }
    }
}
