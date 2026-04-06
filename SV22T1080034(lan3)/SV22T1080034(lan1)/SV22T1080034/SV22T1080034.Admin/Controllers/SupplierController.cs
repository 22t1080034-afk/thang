using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;
using System.Threading.Tasks;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class SupplierController : Controller
    {
        public const string SEARCH_SUPPLIER = "SearchSupplier";

        /// <summary>
        /// Nhập đầu vào tìm kiếm
        /// </summary>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_SUPPLIER);
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
        /// Tìm kiếm và phân trang
        /// </summary>
        public async Task<IActionResult> Search(PaginationSearchInput input)
        {
            await Task.Delay(500);
            //Tìm kiếm
            var result = await PartnerDataService.ListSuppliersAsync(input);

            //Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(SEARCH_SUPPLIER, input);

            //Trả về kết quả cho view
            return View(result);
        }
        public IActionResult Create()
        {
            ViewBag.Title = "Thêm nhà cung cấp";
            var model = new Supplier()
            {
                SupplierID = 0
            };
            return View("Edit", model);
        }
        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật thông tin nhà cung cấp";
            var model = await PartnerDataService.GetSupplierAsync(id);
            if(model == null)
                return RedirectToAction("Index");
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Supplier data)
        {
            ViewBag.Title = data.SupplierID == 0 ? "Thêm nhà cung cấp" : "Cập nhật thông tin nhà cung cấp";

            //Kiểm tra tính hop lệ của dữ liệu đầu vào
            if (string.IsNullOrWhiteSpace(data.SupplierName))
                ModelState.AddModelError(nameof(data.SupplierName), "Vui lòng nhập tên nhà cung cấp");

            if (string.IsNullOrWhiteSpace(data.Province))
                ModelState.AddModelError(nameof(data.Province), "Vui lòng chọn Tỉnh/Thành phố");

            // Email nhà cung cấp KHÔNG bắt buộc, nhưng nếu có nhập thì phải kiểm tra trùng lặp
            if (!string.IsNullOrWhiteSpace(data.Email))
            {
                bool isValidEmail = await PartnerDataService.ValidateSupplierEmailAsync(data.Email, data.SupplierID);
                if (!isValidEmail)
                    ModelState.AddModelError(nameof(data.Email), "Email này đã được sử dụng bởi nhà cung cấp khác");
            }

            // Điều chỉnh dữ liệu (Gán chuỗi rỗng cho các trường không bắt buộc để DB không báo lỗi)
            if (string.IsNullOrWhiteSpace(data.ContactName)) data.ContactName = "";
            if (string.IsNullOrWhiteSpace(data.Phone)) data.Phone = "";
            if (string.IsNullOrWhiteSpace(data.Address)) data.Address = "";
            if (string.IsNullOrWhiteSpace(data.Email)) data.Email = "";

            //Nếu có lỗi thì thông báo cho nguoi dùng (qua View), không luu du liệu
            if (!ModelState.IsValid)
            {
                return View("Edit", data);
            }

            //Luu du liệu vào CSDL
            if (data.SupplierID == 0)
            {
                await PartnerDataService.AddSupplierAsync(data);
                PaginationSearchInput input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = data.SupplierName
                };
                ApplicationContext.SetSessionData(SEARCH_SUPPLIER, input);
            }
            else
            {
                await PartnerDataService.UpdateSupplierAsync(data);
            }
            return RedirectToAction("Index");
        }
        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteSupplierAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetSupplierAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            bool allowDelete = !(await PartnerDataService.IsUsedSupplierAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }

    }
}
