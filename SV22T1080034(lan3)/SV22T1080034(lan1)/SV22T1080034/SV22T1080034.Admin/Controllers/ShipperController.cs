using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize(Roles = $"{WebUserRoles.Administrator},{WebUserRoles.DataManager}")]
    public class ShipperController : Controller
    {
        public const string SEARCH_SHIPPER = "SearchShipper";

        /// <summary>
        /// Tìm kiếm, hiển thị danh sách loại hàng
        /// </summary>
        /// <returns></returns>
        public IActionResult Index()
        {
            var input = ApplicationContext.GetSessionData<PaginationSearchInput>(SEARCH_SHIPPER);
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
            var result = await PartnerDataService.ListShippersAsync(input);

            //Lưu lại điều kiện tìm kiếm vào session
            ApplicationContext.SetSessionData(SEARCH_SHIPPER, input);

            //Trả về kết quả cho view
            return View(result);
        }

        public IActionResult Create()
        {
            ViewBag.Title = "Thêm người giao hàng";
            var model = new Shipper()
            {
                ShipperID = 0
            };
            return View("Edit", model);
        }

        public async Task<IActionResult> Edit(int id)
        {
            ViewBag.Title = "Cập nhật người giao hàng";

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> SaveData(Shipper data)
        {
            ViewBag.Title = data.ShipperID == 0 ? "Thêm người giao hàng" : "Cập nhật người giao hàng";

            // Validate
            if (string.IsNullOrWhiteSpace(data.ShipperName))
                ModelState.AddModelError(nameof(data.ShipperName), "Vui lòng nhập tên");

            if (string.IsNullOrWhiteSpace(data.Phone))
                ModelState.AddModelError(nameof(data.Phone), "Vui lòng nhập số điện thoại");

            if (!ModelState.IsValid)
                return View("Edit", data);

            // Save
            if (data.ShipperID == 0)
            {
                await PartnerDataService.AddShipperAsync(data);
                PaginationSearchInput input = new PaginationSearchInput()
                {
                    Page = 1,
                    PageSize = ApplicationContext.PageSize,
                    SearchValue = data.ShipperName
                };
                ApplicationContext.SetSessionData(SEARCH_SHIPPER, input);
            }
            else
            {
                await PartnerDataService.UpdateShipperAsync(data);
            }

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (Request.Method == "POST")
            {
                await PartnerDataService.DeleteShipperAsync(id);
                return RedirectToAction("Index");
            }

            var model = await PartnerDataService.GetShipperAsync(id);
            if (model == null)
                return RedirectToAction("Index");

            bool allowDelete = !(await PartnerDataService.IsUsedShipperAsync(id));
            ViewBag.AllowDelete = allowDelete;

            return View(model);
        }
    }
}