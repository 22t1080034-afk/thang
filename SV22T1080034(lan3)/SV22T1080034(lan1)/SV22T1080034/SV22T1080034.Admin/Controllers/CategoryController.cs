using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.Admin.Controllers
{
    /// <summary>
    /// Các chức năng liên quan đến loại hàng
    /// </summary>
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class CategoryController : Controller
    {
        public const int PAGESIZE = 10;
        public const string SEARCH_CATEGORY = "SearchCategory";
        /// <summary>
        /// Tìm kiếm, hiển thị danh sách loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_CATEGORY);
            if (input == null)
            {
                input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
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
            var result = await CatalogDataService.ListCategoriesAsync(input);

            //Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(SEARCH_CATEGORY, input);

            //Trả về kết quả cho view
            return View(result);
        }
        /// <summary>
        /// Tạo loại hàng mới
        /// </summary>
        /// <returns></returns>
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm loại hàng";
            var model = new Category()
            {
                CategoryID = 0
            };
            return View("Edit", model);
        }
        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin loại hàng";
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        /// <summary>
        /// Lưu dữ liệu (Xử lý cho cả Create và Edit)
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> SaveData(Category data)
        {
            ViewBag.Title = data.CategoryID == 0 ? "Thêm loại hàng" : "Cập nhật thông tin loại hàng";

            // Kiểm tra tính hợp lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(data.CategoryName))
                ModelState.AddModelError(nameof(data.CategoryName), "Vui lòng nhập tên loại hàng");

            if (string.IsNullOrEmpty(data.Description))
                data.Description = "";

            // Nếu có lỗi, trả về lại giao diện Edit
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            // Lưu dữ liệu vào CSDL
            if (data.CategoryID == 0)
            {
                await CatalogDataService.AddCategoryAsync(data);
                // Sau khi thêm thành công, chuyển session search về trang 1 và tìm đúng tên vừa thêm
                PaginationSearchInput input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = PAGESIZE,
                    SearchValue = data.CategoryName
                };
                ApplicationContext.SetSessionData(SEARCH_CATEGORY, input);
            }
            else
            {
                await CatalogDataService.UpdateCategoryAsync(data);
            }

            return RedirectToAction("Index");
        }
        /// <summary>
        /// Xóa loại hàng
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public async Task<IActionResult> Delete(int id)
        {
            // Xử lý khi xác nhận xóa (POST)
            if (Request.Method == "POST")
            {
                await CatalogDataService.DeleteCategoryAsync(id);
                return RedirectToAction("Index");
            }

            // Hiển thị giao diện xác nhận xóa (GET)
            var model = await CatalogDataService.GetCategoryAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            // Kiểm tra xem loại hàng có đang được sử dụng ở bảng Product không
            bool allowDelete = !(await CatalogDataService.IsUsedCategoryAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }
    }
}
