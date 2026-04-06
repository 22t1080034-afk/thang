using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class CustomerRepository : BaseRepository, ICustomerRepository
    {
        public CustomerRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Customer data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO Customers(CustomerName, ContactName, Province, Address, Phone, Email, Password)
                           VALUES(@CustomerName,@ContactName,@Province,@Address,@Phone,@Email,@Password);
                           SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "DELETE FROM Customers WHERE CustomerID=@id";

            return await connection.ExecuteAsync(sql, new { id }) > 0;
        }

        public async Task<Customer?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "SELECT * FROM Customers WHERE CustomerID=@id";

            return await connection.QueryFirstOrDefaultAsync<Customer>(sql, new { id });
        }

        public async Task<bool> UpdateAsync(Customer data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Customers
                           SET CustomerName=@CustomerName,
                               ContactName=@ContactName,
                               Province=@Province,
                               Address=@Address,
                               Phone=@Phone,
                               Email=@Email
                           WHERE CustomerID=@CustomerID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> ValidateEmailAsync(string email, int id = 0)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*)
                           FROM Customers
                           WHERE Email=@email AND CustomerID<>@id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, id });

            return count == 0;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();

            string sql = "SELECT COUNT(*) FROM Orders WHERE CustomerID=@id";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { id });

            return count > 0;
        }

        public async Task<PagedResult<Customer>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            string search = $"%{input.SearchValue ?? ""}%";

            string sqlCount = @"SELECT COUNT(*)
                                FROM Customers
                                WHERE CustomerName LIKE @search
                                   OR ContactName LIKE @search";

            string sqlData = @"SELECT *
                               FROM Customers
                               WHERE CustomerName LIKE @search
                                  OR ContactName LIKE @search
                               ORDER BY CustomerName
                               OFFSET @offset ROWS
                               FETCH NEXT @pagesize ROWS ONLY";

            int count = await connection.ExecuteScalarAsync<int>(sqlCount, new { search });

            var data = await connection.QueryAsync<Customer>(sqlData, new
            {
                search,
                offset = (input.Page - 1) * input.PageSize,
                pagesize = input.PageSize
            });

            return new PagedResult<Customer>
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }
        public async Task<bool> ChangePasswordAsync(string email, string password)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Customers
                   SET Password = @password
                   WHERE Email = @email";

            return await connection.ExecuteAsync(sql, new { email, password }) > 0;
        }
        public async Task<bool> VerifyPasswordAsync(string email, string password)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*)
                   FROM Customers
                   WHERE Email = @email AND Password = @password";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, password });

            return count > 0;
        }
    }
}