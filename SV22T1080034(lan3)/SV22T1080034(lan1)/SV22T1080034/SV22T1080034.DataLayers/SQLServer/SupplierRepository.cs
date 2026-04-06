using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến nhà cung cấp
    /// </summary>
    public class SupplierRepository : BaseRepository, ISupplierRepository
    {
        /// <summary>
        /// Constructor
        /// </summary>
        public SupplierRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Thêm nhà cung cấp mới
        /// </summary>
        public async Task<int> AddAsync(Supplier data)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"INSERT INTO Suppliers
                        (SupplierName, ContactName, Province, Address, Phone, Email)
                        VALUES
                        (@SupplierName, @ContactName, @Province, @Address, @Phone, @Email);
                        SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@SupplierName", data.SupplierName);
            command.Parameters.AddWithValue("@ContactName", data.ContactName);
            command.Parameters.AddWithValue("@Province", data.Province ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Address", data.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Phone", data.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", data.Email ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        /// <summary>
        /// Xóa nhà cung cấp
        /// </summary>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"DELETE FROM Suppliers WHERE SupplierID=@SupplierID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SupplierID", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        /// <summary>
        /// Lấy thông tin nhà cung cấp
        /// </summary>
        public async Task<Supplier?> GetAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"SELECT * FROM Suppliers WHERE SupplierID=@SupplierID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SupplierID", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Supplier()
                {
                    SupplierID = Convert.ToInt32(reader["SupplierID"]),
                    SupplierName = reader["SupplierName"].ToString() ?? "",
                    ContactName = reader["ContactName"].ToString() ?? "",
                    Province = reader["Province"]?.ToString(),
                    Address = reader["Address"]?.ToString(),
                    Phone = reader["Phone"]?.ToString(),
                    Email = reader["Email"]?.ToString()
                };
            }

            return null;
        }

        /// <summary>
        /// Kiểm tra nhà cung cấp có đang được sử dụng hay không
        /// </summary>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"SELECT COUNT(*) FROM Products WHERE SupplierID=@SupplierID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SupplierID", id);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        /// <summary>
        /// Lấy danh sách nhà cung cấp dưới dạng phân trang
        /// </summary>
        public async Task<PagedResult<Supplier>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            string sqlCount = @"
                                SELECT COUNT(*)
                                FROM Suppliers
                                WHERE SupplierName LIKE @SearchValue";

            int count = await connection.ExecuteScalarAsync<int>(
                sqlCount,
                new { SearchValue = $"%{input.SearchValue}%" }   
            );

            string sqlData = @"
                                SELECT *
                                FROM Suppliers
                                WHERE SupplierName LIKE @SearchValue
                                ORDER BY SupplierName
                                OFFSET @Offset ROWS
                                FETCH NEXT @PageSize ROWS ONLY";

            var data = await connection.QueryAsync<Supplier>(
                sqlData,
                new
                {
                    SearchValue = $"%{input.SearchValue}%",
                    Offset = (input.Page - 1) * input.PageSize,
                    PageSize = input.PageSize
                });

            return new PagedResult<Supplier>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        /// <summary>
        /// Cập nhật nhà cung cấp
        /// </summary>
        public async Task<bool> UpdateAsync(Supplier data)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"UPDATE Suppliers
                        SET SupplierName=@SupplierName,
                            ContactName=@ContactName,
                            Province=@Province,
                            Address=@Address,
                            Phone=@Phone,
                            Email=@Email
                        WHERE SupplierID=@SupplierID";

            using var command = new SqlCommand(sql, connection);

            command.Parameters.AddWithValue("@SupplierID", data.SupplierID);
            command.Parameters.AddWithValue("@SupplierName", data.SupplierName);
            command.Parameters.AddWithValue("@ContactName", data.ContactName);
            command.Parameters.AddWithValue("@Province", data.Province ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Address", data.Address ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Phone", data.Phone ?? (object)DBNull.Value);
            command.Parameters.AddWithValue("@Email", data.Email ?? (object)DBNull.Value);

            return await command.ExecuteNonQueryAsync() > 0;
        }
        public async Task<bool> ValidateEmailAsync(string email, int supplierID = 0)
        {
            using var connection = GetConnection();

            string sql = @"
        SELECT COUNT(*)
        FROM Suppliers
        WHERE Email = @email AND SupplierID <> @supplierID";

            int count = await connection.ExecuteScalarAsync<int>(sql, new { email, supplierID });

            return count == 0;
        }
    }
}