using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SV22T1080034.BusinessLayers;
using SV22T1080034.DomainModels.Partner;
using SV22T1080034.DomainModels.Security;
using System.Security.Claims;
using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.Shop.Controllers
{
    public class AccountController : Controller
    {
        [AllowAnonymous]
        public IActionResult Login()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ email và mật khẩu.";
                return View();
            }

            try
            {
                string hashedPassword = CryptHelper.HashMD5(password);

                var userAccount = await PartnerDataService.AuthorizeCustomerAsync(email, hashedPassword);

                if (userAccount != null)
                {
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, userAccount.UserId),
                        new Claim(ClaimTypes.Name, userAccount.DisplayName),
                        new Claim("Email", userAccount.Email),
                        new Claim("UserId", userAccount.UserId)
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties { IsPersistent = true };

                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity),
                        authProperties);

                    TempData["SuccessMessage"] = $"Chào mừng {userAccount.DisplayName} trở lại!";
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["ErrorMessage"] = "Email hoặc mật khẩu không đúng.";
                    return View();
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return View();
            }
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(
            string customerName,
            string email,
            string phone,
            string password,
            string confirmPassword)
        {
            if (string.IsNullOrWhiteSpace(customerName) ||
                string.IsNullOrWhiteSpace(email) ||
                string.IsNullOrWhiteSpace(password))
            {
                TempData["ErrorMessage"] = "Vui lòng điền đầy đủ thông tin bắt buộc.";
                return View();
            }

            if (password != confirmPassword)
            {
                TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                return View();
            }

            if (password.Length < 6)
            {
                TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự.";
                return View();
            }

            try
            {
                bool emailValid = await PartnerDataService.ValidateCustomerEmailAsync(email, 0);
                if (!emailValid)
                {
                    TempData["ErrorMessage"] = "Email đã được sử dụng.";
                    return View();
                }

                string hashedPassword = CryptHelper.HashMD5(password);

                var newCustomer = new Customer
                {
                    CustomerName = customerName,
                    ContactName = customerName,
                    Email = email,
                    Phone = phone,
                    Province = null,
                    Address = null,
                    Password = hashedPassword,
                    IsLocked = false
                };

                int result = await PartnerDataService.AddCustomerAsync(newCustomer);

                if (result > 0)
                {
                    TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                else
                {
                    TempData["ErrorMessage"] = "Đăng ký thất bại. Vui lòng thử lại.";
                    return View();
                }
            }
            catch
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra. Vui lòng thử lại.";
                return View();
            }
        }

        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Profile()
        {
            int customerId = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out customerId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }
            var customer = await PartnerDataService.GetCustomerAsync(customerId);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Profile(
            string customerName,
            string phone,
            string currentPassword,
            string newPassword,
            string confirmPassword)
        {
            if (User.Identity?.IsAuthenticated != true)
            {
                return RedirectToAction("Login");
            }

            int customerId = 0;
            if (!int.TryParse(User.FindFirst("UserId")?.Value, out customerId))
            {
                TempData["ErrorMessage"] = "Không xác định được người dùng. Vui lòng đăng nhập lại.";
                return RedirectToAction("Login");
            }

            try
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerId);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    return RedirectToAction("Index", "Home");
                }

                // Cập nhật thông tin cơ bản
                customer.CustomerName = customerName;
                customer.Phone = phone;

                // Xử lý đổi mật khẩu nếu có
                if (!string.IsNullOrWhiteSpace(newPassword))
                {
                    if (newPassword != confirmPassword)
                    {
                        TempData["ErrorMessage"] = "Mật khẩu xác nhận không khớp.";
                        return View(customer);
                    }

                    if (newPassword.Length < 6)
                    {
                        TempData["ErrorMessage"] = "Mật khẩu phải có ít nhất 6 ký tự.";
                        return View(customer);
                    }

                    // Kiểm tra mật khẩu hiện tại
                    string hashedCurrent = CryptHelper.HashMD5(currentPassword);
                    var currentUser = await PartnerDataService.AuthorizeCustomerAsync(customer.Email, hashedCurrent);
                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                        return View(customer);
                    }

                    // Đổi mật khẩu
                    string hashedNewPassword = CryptHelper.HashMD5(newPassword);
                    var customerAccountRepo = new SV22T1080034.DataLayers.SQLServer.CustomerAccountRepository(
                        SV22T1080034.BusinessLayers.Configuration.ConnectionString);
                    bool changed = await customerAccountRepo.ChangePasswordAsync(customer.Email, hashedNewPassword);
                    if (!changed)
                    {
                        TempData["ErrorMessage"] = "Đổi mật khẩu thất bại.";
                        return View(customer);
                    }

                    TempData["SuccessMessage"] = "Cập nhật thông tin và đổi mật khẩu thành công!";
                }
                else
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                }

                // Cập nhật thông tin khách hàng
                bool updated = await PartnerDataService.UpdateCustomerAsync(customer);
                if (!updated)
                {
                    TempData["ErrorMessage"] = "Cập nhật thất bại.";
                    return View(customer);
                }

                // Cập nhật lại claims nếu tên thay đổi
                if (User.Identity.IsAuthenticated)
                {
                    var identity = (ClaimsIdentity)User.Identity;
                    identity.RemoveClaim(identity.FindFirst(ClaimTypes.Name));
                    identity.AddClaim(new Claim(ClaimTypes.Name, customer.CustomerName));
                    await HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                }

                return RedirectToAction(nameof(Profile));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile POST Error] {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật.";
                return RedirectToAction(nameof(Profile));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            TempData["SuccessMessage"] = "Đã đăng xuất thành công.";
            return RedirectToAction("Index", "Home");
        }
    }
}
