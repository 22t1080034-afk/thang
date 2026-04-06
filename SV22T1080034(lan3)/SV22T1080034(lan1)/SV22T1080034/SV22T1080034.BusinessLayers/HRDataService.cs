using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DataLayers.SQLServer;
using SV22T1080034.DomainModels;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.HR;

namespace SV22T1080034.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến nhân sự của hệ thống  
    /// </summary>
    public static class HRDataService
    {
        private static readonly IEmployeeRepository employeeDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static HRDataService()
        {
            employeeDB = new EmployeeRepository(Configuration.ConnectionString);
        }

        #region Employee

        /// <summary>
        /// Tìm kiếm và lấy danh sách nhân viên dưới dạng phân trang.
        /// </summary>
        public static async Task<PagedResult<Employee>> ListEmployeesAsync(PaginationSearchInput input)
        {
            return await employeeDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một nhân viên dựa vào mã nhân viên.
        /// </summary>
        public static async Task<Employee?> GetEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return null;
            return await employeeDB.GetAsync(employeeID);
        }

        /// <summary>
        /// Bổ sung một nhân viên mới vào hệ thống.
        /// </summary>
        public static async Task<int> AddEmployeeAsync(Employee data)
        {
            if (string.IsNullOrWhiteSpace(data.FullName)) return 0;
            if (string.IsNullOrWhiteSpace(data.Email)) return 0;

            bool isValidEmail = await employeeDB.ValidateEmailAsync(data.Email, 0);
            if (!isValidEmail) return 0;

            return await employeeDB.AddAsync(data);
        }

        /// <summary>
        /// Cập nhật thông tin của một nhân viên.
        /// </summary>
        public static async Task<bool> UpdateEmployeeAsync(Employee data)
        {
            if (data.EmployeeID <= 0) return false;
            if (string.IsNullOrWhiteSpace(data.FullName)) return false;
            if (string.IsNullOrWhiteSpace(data.Email)) return false;

            bool isValidEmail = await employeeDB.ValidateEmailAsync(data.Email, data.EmployeeID);
            if (!isValidEmail) return false;

            return await employeeDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa một nhân viên dựa vào mã nhân viên.
        /// </summary>
        public static async Task<bool> DeleteEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return false;

            if (await employeeDB.IsUsedAsync(employeeID))
                return false;

            return await employeeDB.DeleteAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem một nhân viên có đang được sử dụng trong dữ liệu hay không.
        /// </summary>
        public static async Task<bool> IsUsedEmployeeAsync(int employeeID)
        {
            if (employeeID <= 0) return false;
            return await employeeDB.IsUsedAsync(employeeID);
        }

        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không.
        /// </summary>
        public static async Task<bool> ValidateEmployeeEmailAsync(string email, int employeeID = 0)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await employeeDB.ValidateEmailAsync(email, employeeID);
        }

        public static async Task<bool> UpdateEmployeeRolesAsync(string email, string roles)
        {
            if (string.IsNullOrWhiteSpace(email)) return false;
            return await employeeDB.UpdateRolesAsync(email, roles);
        }

        // =========================================================================
        // PHẦN BỔ SUNG: Hàm Login để Controller gọi được
        // =========================================================================
        /// <summary>
        /// Kiểm tra thông tin đăng nhập của nhân viên
        /// </summary>
        public static async Task<Employee?> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return null;

            // Nếu sau này dùng mã hóa MD5 thì mở comment dòng dưới ra:
            // password = CryptHelper.HashMD5(password);

            return await employeeDB.LoginAsync(email, password);
        }

        public static async Task<bool> ChangeEmployeePasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            // Nếu sau này dùng mã hóa MD5 thì mở comment dòng dưới ra:
            // password = CryptHelper.HashMD5(password);

            return await employeeDB.ChangePasswordAsync(email, password);
        }

        public static async Task<bool> VerifyEmployeePasswordAsync(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
                return false;

            // Nếu sau này dùng mã hóa MD5 thì mở comment dòng dưới ra:
            // password = CryptHelper.HashMD5(password);

            return await employeeDB.VerifyPasswordAsync(email, password);
        }
        #endregion
    }
}