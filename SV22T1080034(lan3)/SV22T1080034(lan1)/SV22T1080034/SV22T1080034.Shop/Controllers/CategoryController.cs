using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.Shop.Controllers
{
    public class CategoryController : Controller
    {
        [HttpGet]
        public async Task<IActionResult> ListForShop()
        {
            var input = new PaginationSearchInput
            {
                Page = 1,
                PageSize = 100 // Lấy tối đa 100 categories
            };
            var result = await CatalogDataService.ListCategoriesAsync(input);
            return PartialView("_CategoryCards", result.DataItems);
        }
    }
}
