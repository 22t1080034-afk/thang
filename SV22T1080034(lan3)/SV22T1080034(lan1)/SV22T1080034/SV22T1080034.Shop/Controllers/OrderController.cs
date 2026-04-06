using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Sales;

namespace SV22T1080034.Shop.Controllers
{
    [Authorize]
    public class OrderController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> History(int page = 1, int pageSize = 10)
        {
            int customerID = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out customerID))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var input = new OrderSearchInput
            {
                CustomerID = customerID,
                Page = page,
                PageSize = pageSize
            };

            var result = await SalesDataService.ListOrdersAsync(input);

            System.Diagnostics.Debug.WriteLine($"[Order History] CustomerID: {customerID}, Total orders returned: {result?.RowCount ?? 0}");

            if (result?.DataItems != null)
            {
                var filtered = result.DataItems.Where(o => o.CustomerID == customerID).ToList();
                result.DataItems = filtered;
                result.RowCount = filtered.Count;
            }

            ViewBag.Title = "Lịch sử mua hàng - Thang Shop";
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int currentUserId = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out currentUserId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            if (order.CustomerID != currentUserId)
            {
                System.Diagnostics.Debug.WriteLine($"[Security] User {currentUserId} trying to access order {id} owned by {order.CustomerID}");
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("History");
            }

            var details = await SalesDataService.ListDetailsAsync(id);

            var productPhotos = new Dictionary<int, string>();
            foreach (var item in details)
            {
                try
                {
                    var product = await CatalogDataService.GetProductAsync(item.ProductID);
                    if (product != null && !string.IsNullOrEmpty(product.Photo))
                    {
                        productPhotos[item.ProductID] = product.Photo;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[Order Details] Failed to get photo for product {item.ProductID}: {ex.Message}");
                }
            }
            ViewBag.OrderDetails = details;
            ViewBag.ProductPhotos = productPhotos;
            ViewBag.Title = $"Chi tiết đơn hàng #{id} - Thang Shop";
            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int currentUserId = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out currentUserId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("History");
            }

            if (order.CustomerID != currentUserId)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền hủy đơn hàng này.";
                return RedirectToAction("History");
            }

            bool result = await SalesDataService.CancelOrderAsync(id, null);

            if (result)
            {
                TempData["SuccessMessage"] = "Đã hủy đơn hàng.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn hàng (có thể đơn hàng đã ở trạng thái không cho phép hủy).";
            }

            return RedirectToAction("Details", new { id });
        }

        [HttpGet]
        public async Task<IActionResult> Tracking()
        {
            int customerID = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out customerID))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng.";
                return RedirectToAction("Index", "Home");
            }

            var input = new OrderSearchInput
            {
                CustomerID = customerID,
                Page = 1,
                PageSize = 20
            };

            var result = await SalesDataService.ListOrdersAsync(input);

            if (result?.DataItems != null)
            {
                var filtered = result.DataItems.Where(o => o.CustomerID == customerID).ToList();
                result.DataItems = filtered;
                result.RowCount = filtered.Count;
            }

            ViewBag.Title = "Theo dõi đơn hàng - Thang Shop";
            return View(result);
        }
    }
}
