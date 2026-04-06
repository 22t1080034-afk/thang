using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Catalog;
using SV22T1080034.Shop.Services;
using Microsoft.Extensions.Logging;
using System.Net.Mail;
using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.Shop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly INewsletterService _newsletterService;

        public HomeController(ILogger<HomeController> logger, INewsletterService newsletterService)
        {
            _logger = logger;
            _newsletterService = newsletterService;
        }

        public async Task<IActionResult> Index()
        {
            var categoryInput = new PaginationSearchInput() { Page = 1, PageSize = 1000 };
            var categories = await CatalogDataService.ListCategoriesAsync(categoryInput);

            var randomInput = new ProductSearchInput()
            {
                Page = 1,
                PageSize = 8,
                SearchValue = ""
            };
            var randomProducts = await CatalogDataService.ListProductsAsync(randomInput);
            var shuffled = randomProducts.DataItems.OrderBy(x => Guid.NewGuid()).Take(8).ToList();

            ViewBag.Title = "Trang chủ - Thang Shop";
            ViewBag.Categories = categories.DataItems;
            ViewBag.RandomProducts = shuffled;
            return View();
        }

        public IActionResult About()
        {
            ViewBag.Title = "Giới thiệu";
            return View();
        }

        public IActionResult Contact()
        {
            ViewBag.Title = "Liên hệ";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Subscribe(string email)
        {
            // Validate email format
            if (string.IsNullOrWhiteSpace(email) || !IsValidEmail(email))
            {
                TempData["ErrorMessage"] = "Email không hợp lệ.";
                return RedirectToAction("Index", "Home");
            }

            try
            {
                bool success = await _newsletterService.SubscribeAsync(email);
                if (success)
                {
                    _logger?.LogInformation("Newsletter subscription: {Email}", email);
                    TempData["SuccessMessage"] = "Đăng ký nhận tin thành công! Cảm ơn bạn.";
                }
                else
                {
                    TempData["InfoMessage"] = "Email này đã được đăng ký trước đó.";
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Newsletter subscription failed for {Email}", email);
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
            }

            return RedirectToAction("Index", "Home");
        }

        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
