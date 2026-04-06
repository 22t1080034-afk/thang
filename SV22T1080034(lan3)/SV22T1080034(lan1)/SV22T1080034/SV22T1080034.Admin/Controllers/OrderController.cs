using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Sales;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class OrderController : Controller
    {
        public const string SEARCH_ORDER = "SearchOrder";
        public const string SEARCH_PRODUCT = "SearchProduct";
        public const string DRAFT_ORDER = "DraftOrder";

        #region Các chức năng tìm kiếm đơn hàng
        // Order/Index
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<OrderSearchInput>(SEARCH_ORDER);

            if (input == null)
            {
                input = new OrderSearchInput()
                {
                    Page = 1,
                    PageSize = 10,
                    SearchValue = "",
                    Status = null,
                    DateFrom = null,
                    DateTo = null
                };
            }

            return View(input);
        }

        // Order/Search
        public async Task<IActionResult> Search(OrderSearchInput input)
        {
            if (input.PageSize <= 0)
                input.PageSize = 10;

            var result = await SalesDataService.ListOrdersAsync(input);

            ApplicationContext.SetSessionData(SEARCH_ORDER, input);

            return View(result);
        }
        #endregion

        #region Các chức năng liên quan đến tạo đơn hàng mới
        // Order/Create
        public IActionResult Create()
        {
            var input = ApplicationContext.GetSessionData<ProductSearchInput>(SEARCH_PRODUCT);
            if (input == null)
                input = new ProductSearchInput()
                {
                    Page = 1,
                    PageSize = 3,
                    CategoryID = 0,
                    SupplierID = 0,
                    MinPrice = null,
                    MaxPrice = null,
                    SearchValue = ""
                };
            var draft = ApplicationContext.GetSessionData<Order>(DRAFT_ORDER);
            ViewBag.DraftOrder = draft;

            return View(input);
        }

        public async Task<IActionResult> SearchProduct(ProductSearchInput input)
        {
            var result = await CatalogDataService.ListProductsAsync(input);
            ApplicationContext.SetSessionData(SEARCH_PRODUCT, input);
            return View(result);
        }

        /// <summary>
        /// Hiển thị giỏ hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult ShowCart()
        {
            var cart = ShoppingCartHelper.GetShoppingCart();
            return View(cart);
        }

        /// <summary>
        /// Bổ sung hàng vào giỏ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddCartItem(int productID, int quantity, decimal salePrice)
        {
            var product = await CatalogDataService.GetProductAsync(productID);
            if (product == null)
                return Json(new ApiResult(0, "Mặt hàng không tồn tại"));

            // 1. Kiểm tra số lượng (phải lớn hơn 0)
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng mặt hàng được thêm phải lớn hơn 0."));
            }

            // 2. Kiểm tra giá bán (không được là số âm)
            if (salePrice < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ (không được nhỏ hơn 0)."));
            }

            var item = new OrderDetailViewInfo()
            {
                ProductID = productID,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Photo = string.IsNullOrEmpty(product.Photo) ? "nophoto.png" : product.Photo,
                Quantity = quantity,
                SalePrice = salePrice
            };

            ShoppingCartHelper.AddItemToCart(item);
            return Json(new ApiResult(1, ""));
        }

        /// <summary>
        /// Cập nhật thông tin của một mặt hàng trong giỏ hàng (Hiển thị form)
        /// </summary>
        public IActionResult EditCartItem(int id = 0, int productId = 0)
        {
            // Lấy mã sản phẩm bất kể View truyền lên biến 'id' hay 'productId'
            int pId = id > 0 ? id : productId;

            var item = ShoppingCartHelper.GetCartItem(pId);
            if (item == null)
            {
                return Content("Không tìm thấy mặt hàng trong giỏ!");
            }
            return PartialView(item);
        }

        /// <summary>
        /// Xóa hàng trong giỏ
        /// </summary>
        public IActionResult DeleteCartItem(int id = 0, int productId = 0)
        {
            // Lấy mã sản phẩm bất kể View truyền lên biến 'id' hay 'productId'
            int pId = id > 0 ? id : productId;

            //POST: Xoá khỏi giỏ
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.RemoveItemFromCart(pId);
                return Json(new ApiResult(1, ""));
            }

            //GET: Hiển thị hộp thoại để xác nhận
            ViewBag.ProductID = pId;
            return PartialView();
        }

        /// <summary>
        /// Cập nhật mặt hàng trong giỏ (Xử lý lưu)
        /// </summary>
        [HttpPost]
        public IActionResult UpdateCartItem(int productId, int quantity, decimal salePrice)
        {
            // 1. Kiểm tra số lượng
            if (quantity <= 0)
            {
                return Json(new ApiResult(0, "Số lượng mặt hàng phải lớn hơn 0. Nếu muốn xóa, vui lòng dùng nút Xóa."));
            }

            // 2. Kiểm tra giá bán
            if (salePrice < 0)
            {
                return Json(new ApiResult(0, "Giá bán không hợp lệ (không được nhỏ hơn 0)."));
            }

            ShoppingCartHelper.UpdateCartItem(productId, quantity, salePrice);
            return Json(new ApiResult(1, ""));
        }

      

        /// <summary>
        /// Xóa sạch giỏ hàng
        /// </summary>
        public IActionResult ClearCart()
        {
            if (Request.Method == "POST")
            {
                ShoppingCartHelper.ClearCart();
                return Json(new ApiResult(1, ""));
            }

            return PartialView();
        }

        /// <summary>
        /// Tạo đơn hàng mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateOrder(int customerID = 0, string province = "", string address = "")
        {
            var cart = ShoppingCartHelper.GetShoppingCart();

            // 1. Kiểm tra tính hợp lệ của dữ liệu
            if (cart.Count == 0) return Json(new ApiResult(0, "Giỏ hàng đang trống"));
            if (customerID == 0) return Json(new ApiResult(0, "Vui lòng chọn khách hàng"));
            if (string.IsNullOrWhiteSpace(province)) return Json(new ApiResult(0, "Vui lòng chọn Tỉnh/Thành phố"));
            if (string.IsNullOrWhiteSpace(address)) return Json(new ApiResult(0, "Vui lòng nhập địa chỉ giao hàng"));

            // Lấy mã nhân viên đăng nhập
            int employeeID = Convert.ToInt32(User.GetUserData()?.UserId);

            int orderID = await SalesDataService.AddOrderAsync(customerID, province, address, cart);

            if (orderID > 0)
            {
                // Thành công: Xóa giỏ hàng và báo về View để chuyển trang
                ShoppingCartHelper.ClearCart();
                ApplicationContext.SetSessionData(DRAFT_ORDER, null!);

                return Json(new ApiResult(orderID, ""));
            }
            else
            {
                // Thất bại
                return Json(new ApiResult(0, "Lập đơn hàng thất bại. Vui lòng thử lại sau."));
            }
        }

        [HttpPost]
        public IActionResult SaveDraftOrder(int customerID = 0, string province = "", string address = "")
        {
            // Tạo 1 object Order tạm để lưu thông tin nháp
            var draft = new Order
            {
                CustomerID = customerID == 0 ? null : customerID,
                DeliveryProvince = province,
                DeliveryAddress = address
            };
            ApplicationContext.SetSessionData(DRAFT_ORDER, draft);
            return Json(new { success = true });
        }

        #endregion

        #region Các chức năng xem và xử lý đơn hàng
        // 1. Chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id = 0)
        {
            // 1. Lấy thông tin đơn hàng
            var order = await SalesDataService.GetOrderAsync(id);
            if (order == null)
            {
                return RedirectToAction("Index"); // Nếu không thấy đơn hàng thì quay về danh sách
            }

            // 2. Lấy danh sách mặt hàng thuộc đơn hàng này
            var details = await SalesDataService.ListDetailsAsync(id);

            // Truyền chi tiết qua ViewBag, còn thông tin đơn truyền bằng Model
            ViewBag.Details = details;

            return View(order);
        }

        // 2. Duyệt đơn hàng
        [HttpGet]
        public IActionResult Accept(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Accept(int id, IFormCollection form)
        {
            // Lấy EmployeeID từ User (không kiểm tra validation để tránh lỗi session)
            var userData = User.GetUserData();
            int employeeID = 1; // Mặc định là admin nếu không lấy được

            if (userData != null && !string.IsNullOrEmpty(userData.UserId))
            {
                int.TryParse(userData.UserId, out employeeID);
            }

            // Lấy tên nhân viên
            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            string employeeName = employee?.FullName ?? userData?.DisplayName ?? userData?.UserName ?? "Nhân viên";

            // Thực hiện duyệt đơn
            bool success = await SalesDataService.AcceptOrderAsync(id, employeeID);

            // Thông báo
            if (success)
                TempData["SuccessMessage"] = $"Đơn hàng #{id} đã được duyệt thành công bởi {employeeName}!";
            else
                TempData["ErrorMessage"] = "Không thể duyệt đơn hàng. Đơn hàng không tồn tại hoặc không ở trạng thái Mới.";

            return RedirectToAction("Detail", new { id = id });
        }

        // 3. Chuyển người giao hàng (Đã fix gọi dữ liệu từ Database)
        [HttpGet]
        public async Task<IActionResult> Shipping(int id)
        {
            ViewBag.OrderID = id;

            // Gọi CommonDataService lấy danh sách Shipper
            var shippers = await CommonDataService.ListOfShippersAsync();
            ViewBag.Shippers = shippers; // Truyền qua ViewBag

            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Shipping(int id, int shipperID)
        {
            if (shipperID <= 0)
            {
                TempData["ErrorMessage"] = "Vui lòng chọn người giao hàng!";
                return RedirectToAction("Detail", new { id = id });
            }
            await SalesDataService.ShipOrderAsync(id, shipperID);
            TempData["SuccessMessage"] = "Đơn hàng đã được chuyển giao!";
            return RedirectToAction("Detail", new { id = id });
        }

        // 4. Hoàn tất đơn hàng
        [HttpGet]
        public IActionResult Finish(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Finish(int id, IFormCollection form)
        {
            await SalesDataService.CompleteOrderAsync(id);
            TempData["SuccessMessage"] = "Đơn hàng đã được hoàn tất thành công!";
            return RedirectToAction("Detail", new { id = id });
        }

        // 5. Hủy đơn hàng
        [HttpGet]
        public IActionResult Cancel(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id, IFormCollection form)
        {
            var userData = User.GetUserData();
            int employeeID = 1;

            if (userData != null && !string.IsNullOrEmpty(userData.UserId))
            {
                int.TryParse(userData.UserId, out employeeID);
            }

            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            string employeeName = employee?.FullName ?? userData?.DisplayName ?? userData?.UserName ?? "Nhân viên";

            await SalesDataService.CancelOrderAsync(id, employeeID);

            TempData["SuccessMessage"] = $"Đơn hàng #{id} đã được hủy bởi {employeeName}!";
            return RedirectToAction("Detail", new { id = id });
        }

        // 6. Từ chối đơn hàng
        [HttpGet]
        public IActionResult Reject(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, IFormCollection form)
        {
            var userData = User.GetUserData();
            int employeeID = 1;

            if (userData != null && !string.IsNullOrEmpty(userData.UserId))
            {
                int.TryParse(userData.UserId, out employeeID);
            }

            var employee = await HRDataService.GetEmployeeAsync(employeeID);
            string employeeName = employee?.FullName ?? userData?.DisplayName ?? userData.UserName ?? "Nhân viên";

            await SalesDataService.RejectOrderAsync(id, employeeID);

            TempData["SuccessMessage"] = $"Đơn hàng #{id} đã được từ chối bởi {employeeName}!";
            return RedirectToAction("Detail", new { id = id });
        }

        // 7. Xóa đơn hàng
        [HttpGet]
        public IActionResult Delete(int id)
        {
            ViewBag.OrderID = id;
            return PartialView();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id, IFormCollection form)
        {
            bool result = await SalesDataService.DeleteOrderAsync(id);
            if (result)
            {
                TempData["SuccessMessage"] = "Đơn hàng đã được xóa thành công!";
                return RedirectToAction("Index");
            }

            TempData["ErrorMessage"] = "Không thể xóa đơn hàng. Đơn hàng phải ở trạng thái Mới, Đã hủy hoặc Từ chối mới có thể xóa.";
            return RedirectToAction("Detail", new { id = id });
        }

        // 8. Hiển thị Popup sửa mặt hàng trong đơn
        [HttpGet]
        public async Task<IActionResult> EditDetail(int id = 0, int productId = 0)
        {
            var detail = await SalesDataService.GetDetailAsync(id, productId);
            if (detail == null)
            {
                return Content("Không tìm thấy thông tin mặt hàng này!");
            }
            return PartialView(detail);
        }

        // 9. Xử lý khi bấm nút "Lưu thay đổi" trên Popup sửa
        [HttpPost]
        public async Task<IActionResult> UpdateDetail(int orderID, int productID, int quantity, decimal salePrice)
        {
            var data = new OrderDetail()
            {
                OrderID = orderID,
                ProductID = productID,
                Quantity = quantity,
                SalePrice = salePrice
            };
            await SalesDataService.UpdateDetailAsync(data);

            return RedirectToAction("Detail", new { id = orderID });
        }

        // 10. Hiển thị Popup hỏi xác nhận xóa mặt hàng khỏi đơn
        [HttpGet]
        public IActionResult DeleteDetail(int id = 0, int productId = 0)
        {
            ViewBag.OrderID = id;
            ViewBag.ProductID = productId;
            return PartialView();
        }

        // 11. Xử lý khi bấm "Xác nhận xóa"
        [HttpPost]
        [ActionName("DeleteDetail")]
        public async Task<IActionResult> ConfirmDeleteDetail(int id, int productId)
        {
            // id tương ứng với mã đơn hàng (OrderID), productId tương ứng với mã sản phẩm
            await SalesDataService.DeleteDetailAsync(id, productId);

            // Tải lại trang Chi tiết
            return RedirectToAction("Detail", new { id = id });
        }
        #endregion
    }
}