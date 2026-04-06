using SV22T1080034.DomainModels.HR;
using SV22T1080034.DomainModels.Security;

namespace SV22T1080034.DataLayers.Interfaces
{
    /// <summary>
    /// Định nghĩa các phép xử lý dữ liệu trên Employee
    /// </summary>
    public interface IEmployeeRepository : IGenericRepository<Employee>
    {
        /// <summary>
        /// Kiểm tra xem email của nhân viên có hợp lệ không
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <param name="employeeID">
        /// Nếu id = 0: Kiểm tra email của nhân viên mới
        /// Nếu id <> 0: Kiểm tra email của nhân viên có mã là id
        /// </param>
        /// <returns></returns>
        Task<bool> ValidateEmailAsync(string email, int employeeID);

        Task<bool> UpdateRolesAsync(string email, string roles);

        Task<bool> ChangePasswordAsync(string email, string password);

        Task<bool> VerifyPasswordAsync(string email, string password);

        // ========================================================
        // ĐÃ THÊM LẠI HÀM NÀY ĐỂ HRDataService CÓ THỂ GỌI ĐƯỢC:
        // ========================================================
        /// <summary>
        /// Kiểm tra thông tin đăng nhập của nhân viên
        /// </summary>
        Task<Employee?> LoginAsync(string email, string password);
    }
}