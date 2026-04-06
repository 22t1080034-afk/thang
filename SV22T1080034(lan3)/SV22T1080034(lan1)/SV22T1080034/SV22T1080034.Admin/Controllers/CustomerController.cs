using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.Admin.Controllers
{
    /// <summary>
    /// Các chuc nang liên quan đến khách hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.Sales}")]
    public class CustomerController : Controller
    {
        public const string SEARCH_CUSTOMER = "SearchCustomer";
        /// <summary>
        /// Nhập đầu vào tìm kiếm và hiển thị kết quả tìm
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_CUSTOMER);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = ""
                };
            }
            return View(input);
        }
        /// <summary>
        /// Tìm kiếm và trả về kết quả phân trang
        /// </summary>
        /// <param name="input"></param>
        /// <returns></returns>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            //Tìm kiếm
            var result = await PartnerDataService.ListCustomersAsync(input);

            //Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);

            //Trả về kết quả cho view
            return View(result);
        }

        /// <summary>
        /// Tạo khách hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm khách hàng";
            var model = new Customer()
            {
                CustomerID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin khách hàng";
            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }
        /// <summary>
        /// Luu du liệu
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Customer data)
        {
            ViewBag.Title = data.CustomerID == 0 ? "Bổ sung khách hàng" : "Cập nhật thông tin khách hàng";

            //Kiểm tra tính hop lệ của dữ liệu đầu vào
            //Su dụng ModelState để kiểm soát lỗi và thông báo lỗi(Chỉ su dụng khi có model, nếu dùng AJAX thì không dùng)
            if(string.IsNullOrWhiteSpace(data.CustomerName))
                ModelState.AddModelError(nameof(data.CustomerName), "Vui lòng nhập tên khách hàng");

            if(string.IsNullOrWhiteSpace(data.Email))
                ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email khách hàng");
            else if (!(await PartnerDataService.ValidateCustomerEmailAsync(data.Email, data.CustomerID)))
                ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi khách hàng khác");
                
            if(string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành phố");

            //Điều chỉnh du liệu theo logic/quy uoc của hệ thống
            if(string.IsNullOrEmpty(data.ContactName)) data.ContactName = "";
            if(string.IsNullOrEmpty(data.Phone)) data.Phone = "";
            if (string.IsNullOrEmpty(data.Address)) data.Address = "";

            //Nếu có lỗi thì thông báo cho nguoi dùng (qua Vie), không luu du liệu
            if(!ModelState.IsValid)
            {
                return View("Edit", data);
            }


            //Luu du liệu vào CSDL
            if (data.CustomerID == 0)
            {
                await PartnerDataService.AddCustomerAsync(data);
                PaginationSearchInput input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = data.CustomerName
                };
                ApplicationContext.SetSessionData(SEARCH_CUSTOMER, input);
            }
            else
            {
                await PartnerDataService.UpdateCustomerAsync(data);
            }
            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xóa khách hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            if(Request.Method == "POST")
            {
                await PartnerDataService.DeleteCustomerAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            bool allowDelete = !(await PartnerDataService.IsUsedCustomerAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }
        public async Task<IActionResult> ChangePassword(int id)
        {
            ViewBag.Title = "Đổi mật khẩu khách hàng";

            var model = await PartnerDataService.GetCustomerAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string oldPassword, string password, string confirmPassword)
        {
            var customer = await PartnerDataService.GetCustomerAsync(id);
            if (customer == null)
                return RedirectToAction("Index");

            ViewBag.Title = "Đổi mật khẩu khách hàng";

            // 1. Validate rỗng
            if (string.IsNullOrWhiteSpace(oldPassword))
                ModelState.AddModelError("oldPassword", "Vui lòng nhập mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Vui lòng nhập mật khẩu mới");

            if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            // 2. Kiểm tra mật khẩu cũ
            bool isValidOldPassword = await PartnerDataService.VerifyCustomerPasswordAsync(customer.Email, oldPassword);
            if (!isValidOldPassword)
                ModelState.AddModelError("oldPassword", "Mật khẩu cũ không đúng");

            if (!ModelState.IsValid)
                return View(customer);

            // 3. Đổi mật khẩu
            await PartnerDataService.ChangeCustomerPasswordAsync(customer.Email, password);

            return RedirectToAction("Index");
        }
    }
}
