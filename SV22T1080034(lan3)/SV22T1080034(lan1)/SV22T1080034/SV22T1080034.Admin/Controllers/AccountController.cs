using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using System.Security.Claims;

namespace SV22T1080034.Admin.Controllers
{
    [Authorize]
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
            var userAccount = await SV22T1080034.BusinessLayers.HRDataService.Login(username, password);

            if (userAccount != null)
            {
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
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(string oldPassword, string newPassword, string confirmPassword)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu cũ.";
                return View();
            }

            if (string.IsNullOrWhiteSpace(newPassword))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập mật khẩu mới.";
                return View();
            }

            if (newPassword.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu mới phải có ít nhất 6 ký tự.";
                return View();
            }

            if (newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp!";
                return View();
            }

            var email = User.FindFirst("Email")?.Value;
            if (string.IsNullOrEmpty(email))
            {
                TempData["ErrorMessage"] = "Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            // QUAN TRỌNG: Database lưu PLAIN TEXT, không hash
            // Sử dụng trực tiếp mật khẩu người dùng nhập

            // Verify old password
            bool isOldPasswordCorrect = await SV22T1080034.BusinessLayers.HRDataService.VerifyEmployeePasswordAsync(email, oldPassword);
            if (!isOldPasswordCorrect)
            {
                TempData["ErrorMessage"] = "Mật khẩu cũ không đúng!";
                return View();
            }

            // Change password
            bool isSuccess = await SV22T1080034.BusinessLayers.HRDataService.ChangeEmployeePasswordAsync(email, newPassword);
            if (isSuccess)
            {
                // Log out user after password change for security
                await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
                HttpContext.Session.Clear();
                
                TempData["SuccessMessage"] = "Mật khẩu đã được cập nhật thành công! Vui lòng đăng nhập lại với mật khẩu mới.";
                return RedirectToAction("Login");
            }

            TempData["ErrorMessage"] = "Đổi mật khẩu thất bại. Vui lòng thử lại!";
            return View();
        }
    }
}
