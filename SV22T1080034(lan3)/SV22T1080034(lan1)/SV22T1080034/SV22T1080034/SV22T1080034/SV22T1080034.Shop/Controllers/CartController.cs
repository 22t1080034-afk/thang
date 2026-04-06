using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Sales;
using SV22T1080034.Shop.Helpers;
using SV22T1080034.Shop.Models;
using System.Diagnostics;

namespace SV22T1080034.Shop.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private const string CART_SESSION_KEY = "CartItems";

        private List<CartItem> GetCartFromSession()
        {
            var cart = HttpContext.Session.GetObject<List<CartItem>>(CART_SESSION_KEY);
            return cart ?? new List<CartItem>();
        }

        private void SaveCartToSession(List<CartItem> cart)
        {
            HttpContext.Session.SetObject(CART_SESSION_KEY, cart);
        }

        public IActionResult Index()
        {
            var cart = GetCartFromSession();
            ViewBag.Title = "Giỏ hàng - Thang Shop";
            return View(cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Add(int productID, int quantity, decimal salePrice)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"[Cart Add] productID={productID}, quantity={quantity}, salePrice={salePrice}");

                // Validate inputs
                if (productID <= 0)
                {
                    return Json(new { code = 0, message = "Sản phẩm không hợp lệ." });
                }
                if (quantity <= 0)
                {
                    return Json(new { code = 0, message = "Số lượng không hợp lệ." });
                }

                var product = await CatalogDataService.GetProductAsync(productID);
                if (product == null)
                {
                    System.Diagnostics.Debug.WriteLine($"[Cart Add] Product not found: {productID}");
                    return Json(new { code = 0, message = "Không tìm thấy sản phẩm." });
                }

                System.Diagnostics.Debug.WriteLine($"[Cart Add] Found product: {product.ProductName}");

                var cart = GetCartFromSession();
                System.Diagnostics.Debug.WriteLine($"[Cart Add] Current cart items: {cart.Count}");

                var existingItem = cart.FirstOrDefault(c => c.ProductID == productID);
                if (existingItem != null)
                {
                    existingItem.Quantity += quantity;
                    System.Diagnostics.Debug.WriteLine($"[Cart Add] Updated quantity: {existingItem.Quantity}");
                }
                else
                {
                    cart.Add(new CartItem
                    {
                        ProductID = productID,
                        ProductName = product.ProductName,
                        Photo = product.Photo,
                        Price = product.Price,
                        SalePrice = product.Price, // SỬA: Dùng giá từ DB, không dùng từ form (chống gian lận)
                        Quantity = quantity,
                        Unit = product.Unit
                    });
                    System.Diagnostics.Debug.WriteLine($"[Cart Add] Added new item");
                }

                SaveCartToSession(cart);
                System.Diagnostics.Debug.WriteLine($"[Cart Add] Cart saved. Total items: {cart.Sum(c => c.Quantity)}");

                return Json(new { code = 1, message = "Đã thêm vào giỏ hàng!" });
            }
            catch (Exception ex)
            {
                // Log error but don't expose details to client
                System.Diagnostics.Debug.WriteLine($"[Cart Add Error] {ex.GetType().Name}: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Cart Add StackTrace] {ex.StackTrace}");
                return Json(new { code = 0, message = "Có lỗi xảy ra khi thêm vào giỏ. Vui lòng thử lại." });
            }
        }

        [HttpGet]
        public IActionResult Count()
        {
            var cart = GetCartFromSession();
            int count = cart.Sum(c => c.Quantity);
            return Json(new { count = count });
        }

        [HttpGet]
        public IActionResult GetCartModal()
        {
            var cart = GetCartFromSession();
            return PartialView("_CartModal", cart);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Remove(int productID)
        {
            var cart = GetCartFromSession();
            var item = cart.FirstOrDefault(c => c.ProductID == productID);
            if (item != null)
            {
                cart.Remove(item);
                SaveCartToSession(cart);
                TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Update(int productID, int quantity)
        {
            try
            {
                var cart = GetCartFromSession();
                var item = cart.FirstOrDefault(c => c.ProductID == productID);
                if (item != null)
                {
                    if (quantity <= 0)
                    {
                        cart.Remove(item);
                        TempData["SuccessMessage"] = "Đã xóa sản phẩm khỏi giỏ hàng!";
                    }
                    else if (quantity > 999)
                    {
                        TempData["ErrorMessage"] = "Số lượng không hợp lệ (tối đa 999).";
                    }
                    else
                    {
                        item.Quantity = quantity;
                        TempData["SuccessMessage"] = "Đã cập nhật số lượng!";
                    }
                    SaveCartToSession(cart);
                }
                else
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sản phẩm trong giỏ hàng.";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Cart Update Error] {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật. Vui lòng thử lại.";
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Clear()
        {
            HttpContext.Session.Remove(CART_SESSION_KEY);
            TempData["SuccessMessage"] = "Đã xóa tất cả sản phẩm khỏi giỏ hàng!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Checkout(string deliveryProvince, string deliveryAddress, string selectedProductIds)
        {
            var cart = GetCartFromSession();
            if (!cart.Any())
            {
                return Json(new { code = 0, message = "Giỏ hàng trống." });
            }

            // Lọc sản phẩm được chọn
            List<CartItem> selectedCartItems;
            if (string.IsNullOrWhiteSpace(selectedProductIds))
            {
                selectedCartItems = new List<CartItem>();
            }
            else
            {
                var ids = selectedProductIds.Split(',')
                    .Select(id => {
                        int.TryParse(id, out int result);
                        return result;
                    })
                    .Where(id => id > 0)
                    .ToList();

                selectedCartItems = cart.Where(c => ids.Contains(c.ProductID)).ToList();
            }

            if (!selectedCartItems.Any())
            {
                return Json(new { code = 0, message = "Vui lòng chọn ít nhất một sản phẩm để thanh toán." });
            }

            if (string.IsNullOrWhiteSpace(deliveryProvince) || string.IsNullOrWhiteSpace(deliveryAddress))
            {
                return Json(new { code = 0, message = "Vui lòng nhập đầy đủ thông tin giao hàng." });
            }

            int customerID = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out customerID))
            {
                return Json(new { code = 0, message = "Không xác định được người dùng. Vui lòng đăng nhập lại." });
            }

            try
            {
                var orderDetails = selectedCartItems.Select(c => new OrderDetailViewInfo
                {
                    ProductID = c.ProductID,
                    ProductName = c.ProductName,
                    Quantity = c.Quantity,
                    SalePrice = c.SalePrice
                }).ToList();

                int orderID = await SalesDataService.AddOrderAsync(
                    customerID,
                    deliveryProvince,
                    deliveryAddress,
                    orderDetails
                );

                if (orderID > 0)
                {
                    // Xóa các sản phẩm đã đặt khỏi giỏ hàng
                    var selectedIds = selectedCartItems.Select(c => c.ProductID).ToList();
                    cart = cart.Where(c => !selectedIds.Contains(c.ProductID)).ToList();
                    SaveCartToSession(cart);

                    TempData["SuccessMessage"] = "Đặt hàng thành công!";
                    var redirectUrl = Url.Action("Details", "Order", new { id = orderID });
                    return Json(new { code = 1, redirectUrl = redirectUrl });
                }
                else
                {
                    return Json(new { code = 0, message = "Đặt hàng thất bại. Vui lòng thử lại." });
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Checkout Error] {ex.Message}");
                return Json(new { code = 0, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }
    }
}
