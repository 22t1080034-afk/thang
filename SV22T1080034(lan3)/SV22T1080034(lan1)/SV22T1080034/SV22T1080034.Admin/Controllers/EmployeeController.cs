using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.HR;

namespace SV22T1080034.Admin.Controllers
{
    /// <summary>
    /// Quản lý nhân viên
    /// </summary>
    [Authorize]
    public class EmployeeController : Controller
    {
        public const string SEARCH_EMPLOYEE = "SearchEmployee";
        // 1. Khai báo biến _logger
        private readonly ILogger<EmployeeController> _logger;

        // 2. Tạo Constructor để ASP.NET Core tự động "tiêm" dịch vụ Log vào
        public EmployeeController(ILogger<EmployeeController> logger)
        {
            _logger = logger;
        }
        /// <summary>
        /// Tìm kiếm, hiển thị danh sách loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_EMPLOYEE);
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
            var result = await HRDataService.ListEmployeesAsync(input);

            //Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(SEARCH_EMPLOYEE, input);

            //Trả về kết quả cho view
            return View(result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Bổ sung nhân viên";
            var model = new Employee()
            {
                EmployeeID = 0,
                IsWorking = true
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhân viên";
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Employee data, IFormFile? uploadPhoto)
        {
            try
            {
                ViewBag.Title = data.EmployeeID == 0 ? "Bổ sung nhân viên" : "Cập nhật thông tin nhân viên";

                //Kiểm tra dữ liệu đầu vào: FullName và Email là bắt buộc, Email chưa được sử dụng bởi nhân viên khác
                if (string.IsNullOrWhiteSpace(data.FullName))
                    ModelState.AddModelError(nameof(data.FullName), "Vui lòng nhập họ tên nhân viên");

                if (string.IsNullOrWhiteSpace(data.Email))
                    ModelState.AddModelError(nameof(data.Email), "Vui lòng nhập email nhân viên");
                else if (!await HRDataService.ValidateEmployeeEmailAsync(data.Email, data.EmployeeID))
                    ModelState.AddModelError(nameof(data.Email), "Email đã được sử dụng bởi nhân viên khác");

                if (!ModelState.IsValid)
                    return View("Edit", data);

                //Xử lý upload ảnh
                if (uploadPhoto != null)
                {
                    var fileName = $"{Guid.NewGuid()}{Path.GetExtension(uploadPhoto.FileName)}";
                    var filePath = Path.Combine(ApplicationContext.WWWRootPath, "images/employees", fileName);
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await uploadPhoto.CopyToAsync(stream);
                    }
                    data.Photo = fileName;
                }

                //Tiền xử lý dữ liệu trước khi lưu vào database
                if (string.IsNullOrEmpty(data.Address)) data.Address = "";
                if (string.IsNullOrEmpty(data.Phone)) data.Phone = "";
                if (string.IsNullOrEmpty(data.Photo)) data.Photo = "nophoto.png";

                //Lưu dữ liệu vào database (bổ sung hoặc cập nhật)
                if (data.EmployeeID == 0)
                {
                    await HRDataService.AddEmployeeAsync(data);
                    PaginationSearchInput input = new PaginationSearchInput()
                    {
                        Page = 1,
                        PageSize = ApplicationContext.PageSize,
                        SearchValue = data.FullName
                    };
                    ApplicationContext.SetSessionData(SEARCH_EMPLOYEE, input);
                }
                else
                {
                    await HRDataService.UpdateEmployeeAsync(data);
                }
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                // Ghi log lỗi căn cứ vào ex.Message và ex.StackTrace
                // LogError sẽ tự động ghi lại ex.Message và ex.StackTrace vào cửa sổ Console hoặc file log (tùy cấu hình)
                _logger.LogError(ex, "Lỗi khi lưu dữ liệu nhân viên. EmployeeID: {EmployeeID}, Email: {Email}", data.EmployeeID, data.Email);
                ModelState.AddModelError(string.Empty, "Hệ thống đang bận hoặc dữ liệu không hợp lệ. Vui lòng kiểm tra dữ liệu hoặc thử lại sau");
                return View("Edit", data);
            }
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await HRDataService.DeleteEmployeeAsync(id);
                return RedirectToAction("Index");
            }

            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            bool allowDelete = !(await HRDataService.IsUsedEmployeeAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangeRole(int id, List<string> roles)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            // Ghép role thành chuỗi
            string roleNames = string.Join(",", roles ?? new List<string>());

            await HRDataService.UpdateEmployeeRolesAsync(employee.Email, roleNames);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> ChangeRole(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        public async Task<IActionResult> ChangePassword(int id)
        {
            var model = await HRDataService.GetEmployeeAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(int id, string oldPassword, string password, string confirmPassword)
        {
            var employee = await HRDataService.GetEmployeeAsync(id);
            if (employee == null)
                return RedirectToAction("Index");

            if (string.IsNullOrWhiteSpace(oldPassword))
                ModelState.AddModelError("oldPassword", "Vui lòng nhập mật khẩu cũ");

            if (string.IsNullOrWhiteSpace(password))
                ModelState.AddModelError("password", "Vui lòng nhập mật khẩu mới");

            if (password != confirmPassword)
                ModelState.AddModelError("confirmPassword", "Mật khẩu xác nhận không khớp");

            bool valid = await HRDataService.VerifyEmployeePasswordAsync(employee.Email, oldPassword);
            if (!valid)
                ModelState.AddModelError("oldPassword", "Mật khẩu cũ không đúng");

            if (!ModelState.IsValid)
                return View(employee);

            await HRDataService.ChangeEmployeePasswordAsync(employee.Email, password);

            return RedirectToAction("Index");
        }
    }
}