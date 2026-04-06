using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DataLayers.SQLServer;
using SV22T1080034.DomainModels;
using SV22T1080034.DomainModels.Security;

namespace SV22T1080034.BusinessLayers
{
    /// <summary>
    /// Xử lý nghiệp vụ liên quan đến bảo mật (đăng nhập, đổi mật khẩu)
    /// </summary>
    public static class SecurityDataService
    {
        private static readonly IUserAccountRepository customerAccountDB;
        private static readonly IUserAccountRepository employeeAccountDB;

        static SecurityDataService()
        {
            customerAccountDB = new CustomerAccountRepository(Configuration.ConnectionString);
            employeeAccountDB = new EmployeeAccountRepository(Configuration.ConnectionString);
        }

        /// <summary>
        /// Xác thực đăng nhập
        /// </summary>
        public static async Task<UserAccount?> AuthorizeAsync(string userName, string password)
        {
            // 🔐 Hash password trước khi check DB
            string hashedPassword = CryptHelper.HashMD5(password);

            // 1. Check nhân viên trước (admin)
            var user = await employeeAccountDB.Authorize(userName, hashedPassword);
            if (user != null)
                return user;

            // 2. Nếu không phải nhân viên → check customer
            user = await customerAccountDB.Authorize(userName, hashedPassword);
            return user;
        }

        /// <summary>
        /// Đổi mật khẩu
        /// </summary>
        public static async Task<bool> ChangePasswordAsync(string userName, string newPassword)
        {
            string hashedPassword = CryptHelper.HashMD5(newPassword);

            // thử đổi bên employee trước
            if (await employeeAccountDB.ChangePasswordAsync(userName, hashedPassword))
                return true;

            // nếu không có thì đổi bên customer
            return await customerAccountDB.ChangePasswordAsync(userName, hashedPassword);
        }

        /// <summary>
        /// Kiểm tra mật khẩu đúng không
        /// </summary>
        public static async Task<bool> VerifyPasswordAsync(string userName, string password)
        {
            var user = await AuthorizeAsync(userName, password);
            return user != null;
        }
    }
}