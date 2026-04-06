using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

                // DEBUG: Ghi log
                System.Diagnostics.Debug.WriteLine($"[Login Debug] Email: {email}, Input Password: {password}, Hashed: {hashedPassword}");

                // Chỉ kiểm tra tài khoản Customer - không kiểm tra Employee
                var userAccount = await PartnerDataService.AuthorizeCustomerAsync(email, hashedPassword);

                System.Diagnostics.Debug.WriteLine($"[Login Debug] userAccount result: {(userAccount != null ? "FOUND" : "NULL")}");

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

        // GET: /Shop/Account/Register
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

        // GET: /Shop/Account/Profile
        [HttpGet]
        public async Task<IActionResult> Profile()
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
            var customer = await PartnerDataService.GetCustomerAsync(customerId);

            if (customer == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                return RedirectToAction("Index", "Home");
            }

            return View(customer);
        }

        [HttpPost]
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

            System.Diagnostics.Debug.WriteLine($"[Profile POST] CustomerID: {customerId}, Name: {customerName}, Phone: {phone}");

            try
            {
                var customer = await PartnerDataService.GetCustomerAsync(customerId);
                if (customer == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin khách hàng.";
                    System.Diagnostics.Debug.WriteLine($"[Profile POST] Customer not found for ID: {customerId}");
                    return RedirectToAction("Index", "Home");
                }

                System.Diagnostics.Debug.WriteLine($"[Profile POST] Found customer: {customer.CustomerName}, Email: {customer.Email}");

                customer.CustomerName = customerName;
                customer.Phone = phone;

                bool updated = await PartnerDataService.UpdateCustomerAsync(customer);
                System.Diagnostics.Debug.WriteLine($"[Profile POST] Update result: {updated}");

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

                    string hashedCurrent = CryptHelper.HashMD5(currentPassword);
                    var currentUser = await PartnerDataService.AuthorizeCustomerAsync(customer.Email, hashedCurrent);
                    if (currentUser == null)
                    {
                        TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                        return View(customer);
                    }

                    // Change password using CustomerAccountRepository (SỬA: nên inject DI, nhưng giữ nguyên để tránh breaking changes)
                    string hashedNewPassword = CryptHelper.HashMD5(newPassword);
                    var customerAccountRepo = new SV22T1080034.DataLayers.SQLServer.CustomerAccountRepository(
                        SV22T1080034.BusinessLayers.Configuration.ConnectionString);
                    bool changed = await customerAccountRepo.ChangePasswordAsync(customer.Email, hashedNewPassword);
                    if (!changed)
                    {
                        TempData["ErrorMessage"] = "Đổi mật khẩu thất bại.";
                        return View(customer);
                    }
                }

                if (updated)
                {
                    TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                    return RedirectToAction(nameof(Profile));
                }
                else
                {
                    TempData["ErrorMessage"] = "Cập nhật thất bại.";
                    return View(customer);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Profile POST Error] {ex.Message}");
                TempData["ErrorMessage"] = "Có lỗi xảy ra.";
                return View();
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

        [HttpGet]
        public async Task<IActionResult> UpdatePasswords()
        {
            // CHỈ CHẠY TRONG DEVELOPMENT
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
            {
                return Unauthorized("This action is only available in Development environment.");
            }

            int updatedCount = 0;
            int skippedCount = 0;
            var errors = new List<string>();

            try
            {
                var input = new PaginationSearchInput
                {
                    Page = 1,
                    PageSize = 1000
                };

                var customers = await PartnerDataService.ListCustomersAsync(input);
                var allCustomers = customers.DataItems;

                foreach (var customer in allCustomers)
                {
                    try
                    {
                        // Kiểm tra password có phải là MD5 hash (32 ký tự hex) không
                        if (string.IsNullOrWhiteSpace(customer.Password))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Nếu password đã là MD5 (32 hex chars) thì bỏ qua
                        if (customer.Password.Length == 32 && System.Text.RegularExpressions.Regex.IsMatch(customer.Password, @"\A[0-9a-fA-F]{32}\Z"))
                        {
                            skippedCount++;
                            continue;
                        }

                        // Password chưa hash → hash nó
                        string hashedPassword = CryptHelper.HashMD5(customer.Password);

                        // Cập nhật password cho customer
                        var customerToUpdate = new Customer
                        {
                            CustomerID = customer.CustomerID,
                            CustomerName = customer.CustomerName,
                            Email = customer.Email,
                            Phone = customer.Phone,
                            Province = customer.Province,
                            Address = customer.Address,
                            Password = hashedPassword,
                            IsLocked = customer.IsLocked
                        };

                        bool success = await PartnerDataService.UpdateCustomerAsync(customerToUpdate);
                        if (success)
                        {
                            updatedCount++;
                        }
                        else
                        {
                            errors.Add($"Failed to update CustomerID={customer.CustomerID}");
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"Error for CustomerID={customer.CustomerID}: {ex.Message}");
                    }
                }

                var result = new
                {
                    message = "Password update completed.",
                    totalCustomers = allCustomers.Count,
                    updated = updatedCount,
                    skipped = skippedCount,
                    errors = errors
                };

                return Json(result);
            }
            catch (Exception ex)
            {
                return Json(new { error = ex.Message }, JsonSerializerOptions.Default);
            }
        }

        // GET: /Shop/Account/TestLogin?email=xxx&password=xxx (DEVELOPMENT ONLY)
        [HttpGet]
        public async Task<IActionResult> TestLogin(string email, string password)
        {
            // CHỈ CHẠY TRONG DEVELOPMENT
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") != "Development")
            {
                return Unauthorized("This action is only available in Development environment.");
            }

            try
            {
                if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password))
                {
                    return Json(new
                    {
                        success = false,
                        message = "Vui lòng cung cấp email và password."
                    });
                }

                string hashedPassword = CryptHelper.HashMD5(password);
                System.Diagnostics.Debug.WriteLine($"[TestLogin] Email: {email}, Input: {password}, Hashed: {hashedPassword}");
                var user = await PartnerDataService.AuthorizeCustomerAsync(email, hashedPassword);
                System.Diagnostics.Debug.WriteLine($"[TestLogin] user result: {(user != null ? "FOUND" : "NULL")}");

                if (user != null)
                {
                    return Json(new
                    {
                        success = true,
                        message = "Đăng nhập thành công!",
                        user = new
                        {
                            user.UserId,
                            user.UserName,
                            user.DisplayName,
                            user.RoleNames
                        },
                        hashedPassword = hashedPassword
                    });
                }
                else
                {
                    // Thử kiểm tra xem customer có tồn tại không
                    var allCustomers = await PartnerDataService.ListCustomersAsync(new PaginationSearchInput { Page = 1, PageSize = 1000 });
                    var customer = allCustomers.DataItems.FirstOrDefault(c => c.Email == email);

                    if (customer != null)
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Customer tồn tại nhưng password không khớp.",
                            customerInfo = new
                            {
                                customer.CustomerID,
                                customer.CustomerName,
                                customer.Email,
                                passwordInDB = customer.Password,
                                yourHashedPassword = hashedPassword,
                                isMD5 = customer.Password.Length == 32 && System.Text.RegularExpressions.Regex.IsMatch(customer.Password, @"\A[0-9a-fA-F]{32}\Z")
                            }
                        });
                    }
                    else
                    {
                        return Json(new
                        {
                            success = false,
                            message = "Email không tồn tại trong hệ thống.",
                            hashedPassword = hashedPassword
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = "Lỗi: " + ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }
    }
}
