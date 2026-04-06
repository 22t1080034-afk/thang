using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.HR;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class EmployeeRepository : BaseRepository, IEmployeeRepository
    {
        public EmployeeRepository(string connectionString) : base(connectionString) { }

        public async Task<int> AddAsync(Employee data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO Employees(FullName, BirthDate, Address, Phone, Email, Photo, IsWorking)
               VALUES(@FullName, @BirthDate, @Address, @Phone, @Email, @Photo, @IsWorking);
               SELECT SCOPE_IDENTITY();";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            return await connection.ExecuteAsync("DELETE FROM Employees WHERE EmployeeID=@id", new { id }) > 0;
        }

        public async Task<Employee?> GetAsync(int id)
        {
            using var connection = GetConnection();
            return await connection.QueryFirstOrDefaultAsync<Employee>(
                "SELECT * FROM Employees WHERE EmployeeID=@id", new { id });
        }

        public async Task<bool> UpdateAsync(Employee data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Employees
               SET FullName=@FullName,
                   BirthDate=@BirthDate,
                   Address=@Address,
                   Phone=@Phone,
                   Email=@Email,
                   Photo=@Photo,
                   IsWorking=@IsWorking
               WHERE EmployeeID=@EmployeeID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = GetConnection();

            int count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Employees WHERE Email=@email AND EmployeeID<>@id",
                new { email, id });

            return count == 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            int count = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM Orders WHERE EmployeeID=@id", new { id });

            return count > 0;
        }

        public async Task<PagedResult<Employee>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            string sqlCount = "SELECT COUNT(*) FROM Employees WHERE FullName LIKE @search";

            string sqlData = @"SELECT *
                               FROM Employees
                               WHERE FullName LIKE @search
                               ORDER BY FullName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

            int count = await connection.ExecuteScalarAsync<int>(sqlCount,
                new { search = $"%{input.SearchValue}%" });

            var data = await connection.QueryAsync<Employee>(sqlData, new
            {
                search = $"%{input.SearchValue}%",
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize
            });

            return new PagedResult<Employee>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        public async Task<bool> UpdateRolesAsync(string email, string roles)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Employees
                    SET RoleNames = @roles
                    WHERE Email = @email";

            return await connection.ExecuteAsync(sql, new { email, roles }) > 0;
        }

        public async Task<bool> ChangePasswordAsync(string email, string password)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Employees
                    SET Password = @password
                    WHERE Email = @email";

            return await connection.ExecuteAsync(sql, new { email, password }) > 0;
        }

        public async Task<bool> VerifyPasswordAsync(string email, string password)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*)
                    FROM Employees
                    WHERE Email = @email AND Password = @password";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, password });

            return count > 0;
        }

        // ==========================================
        // ĐÂY LÀ HÀM ĐƯỢC BỔ SUNG ĐỂ SỬA LỖI CS1061
        // ==========================================
        public async Task<Employee?> LoginAsync(string email, string password)
        {
            using var connection = GetConnection();

            string sql = @"SELECT * FROM Employees 
                           WHERE Email = @email AND Password = @password";

            return await connection.QueryFirstOrDefaultAsync<Employee>(sql, new { email, password });
        }
    }
}