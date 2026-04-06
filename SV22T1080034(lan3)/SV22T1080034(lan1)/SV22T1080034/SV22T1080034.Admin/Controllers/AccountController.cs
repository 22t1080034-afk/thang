using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using System.Security.Claims;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize] // Mặc định khóa toàn bộ controller này
    public class AccountController : Controller
    {
        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login()
        {
            if (User.Identity!.IsAuthenticated)
                return RedirectToAction("Index", "Home");

            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(string username, string password)
        {
            // ĐÃ SỬA: Phải có "await" vì hàm Login trong HRDataService là async Task
            var userAccount = await SV22T1080034.BusinessLayers.HRDataService.Login(username, password);

            if (userAccount != null)
            {
                // Lưu ý nhỏ: Nếu model Employee của cậu không có "RoleNames" mà dùng tên khác (ví dụ: Role, Roles) thì nhớ sửa lại ở đây cho khớp nhé.
                string roles = string.IsNullOrWhiteSpace(userAccount.RoleNames) ? "" : userAccount.RoleNames;

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, userAccount.FullName),
                    new Claim("Email", userAccount.Email),
                    new Claim("EmployeeID", userAccount.EmployeeID.ToString()),
                    new Claim(ClaimTypes.Role, roles)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties { IsPersistent = true };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                return RedirectToAction("Index", "Home");
            }

            ViewBag.Message = "Tài khoản hoặc mật khẩu không đúng!";
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            HttpContext.Session.Clear();
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Lockscreen()
        {
            ViewBag.Username = User.Identity?.Name ?? "Admin";
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Unlock(string password)
        {
            var email = User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // ĐÃ SỬA: Hàm Unlock (POST) cũng cần đổi thành async Task và thêm await ở đây
            var userAccount = await SV22T1080034.BusinessLayers.HRDataService.Login(email, password);
            if (userAccount != null)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.Username = User.Identity?.Name;
            ViewBag.Message = "Sai mật khẩu!";
            return View("Lockscreen");
        }

        [HttpGet]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            if (newPassword != confirmPassword)
            {
                ViewBag.Message = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var email = User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email)) return RedirectToAction("Login");

            // 1. MÃ HÓA MẬT KHẨU NGAY TẠI TẦNG ADMIN TRƯỚC
            string hashedOldPassword = CryptHelper.HashMD5(oldPassword);
            string hashedNewPassword = CryptHelper.HashMD5(newPassword);

            // 2. TRUYỀN MẬT KHẨU ĐÃ MÃ HÓA XUỐNG CHO HRDataService
            bool isOldPasswordCorrect = await SV22T1080034.BusinessLayers.HRDataService.VerifyEmployeePasswordAsync(email, hashedOldPassword);
            if (!isOldPasswordCorrect)
            {
                ViewBag.Message = "Mật khẩu cũ không đúng!";
                return View();
            }

            bool isSuccess = await SV22T1080034.BusinessLayers.HRDataService.ChangeEmployeePasswordAsync(email, hashedNewPassword);
            if (isSuccess)
            {
                ModelState.Clear();
                ViewBag.SuccessMessage = "Đổi mật khẩu thành công!";
                return View();
            }

            // ĐÃ SỬA LỖI CS0161: Bổ sung return View ở cuối cùng để vét hết các trường hợp thất bại
            ViewBag.Message = "Đổi mật khẩu không thành công. Vui lòng thử lại!";
            return View();
        }
    }
}