using Microsoft.Data.SqlClient;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến người giao hàng
    /// </summary>
    public class ShipperRepository : BaseRepository, IGenericRepository<Shipper>
    {
        public ShipperRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<int> AddAsync(Shipper data)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"INSERT INTO Shippers(ShipperName, Phone)
                        VALUES(@ShipperName, @Phone);
                        SELECT SCOPE_IDENTITY();";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShipperName", data.ShipperName);
            command.Parameters.AddWithValue("@Phone", data.Phone ?? (object)DBNull.Value);

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }

        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"DELETE FROM Shippers WHERE ShipperID=@ShipperID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShipperID", id);

            return await command.ExecuteNonQueryAsync() > 0;
        }

        public async Task<Shipper?> GetAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"SELECT * FROM Shippers WHERE ShipperID=@ShipperID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShipperID", id);

            using var reader = await command.ExecuteReaderAsync();

            if (await reader.ReadAsync())
            {
                return new Shipper()
                {
                    ShipperID = Convert.ToInt32(reader["ShipperID"]),
                    ShipperName = reader["ShipperName"].ToString() ?? "",
                    Phone = reader["Phone"]?.ToString()
                };
            }

            return null;
        }

        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"SELECT COUNT(*) FROM Orders WHERE ShipperID=@ShipperID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShipperID", id);

            int count = Convert.ToInt32(await command.ExecuteScalarAsync());
            return count > 0;
        }

        public async Task<PagedResult<Shipper>> ListAsync(PaginationSearchInput input)
        {
            List<Shipper> data = new List<Shipper>();
            int count = 0;

            using var connection = GetConnection();
            await connection.OpenAsync();

            var sqlCount = @"SELECT COUNT(*) FROM Shippers WHERE ShipperName LIKE @SearchValue";
            using var cmdCount = new SqlCommand(sqlCount, connection);
            cmdCount.Parameters.AddWithValue("@SearchValue", $"%{input.SearchValue}%");

            count = Convert.ToInt32(await cmdCount.ExecuteScalarAsync());

            var sql = @"SELECT * FROM Shippers
                        WHERE ShipperName LIKE @SearchValue
                        ORDER BY ShipperName
                        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@SearchValue", $"%{input.SearchValue}%");
            command.Parameters.AddWithValue("@Offset", (input.Page - 1) * input.PageSize);
            command.Parameters.AddWithValue("@PageSize", input.PageSize);

            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                data.Add(new Shipper()
                {
                    ShipperID = Convert.ToInt32(reader["ShipperID"]),
                    ShipperName = reader["ShipperName"].ToString() ?? "",
                    Phone = reader["Phone"]?.ToString()
                });
            }

            return new PagedResult<Shipper>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data
            };
        }

        public async Task<bool> UpdateAsync(Shipper data)
        {
            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"UPDATE Shippers
                        SET ShipperName=@ShipperName,
                            Phone=@Phone
                        WHERE ShipperID=@ShipperID";

            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@ShipperID", data.ShipperID);
            command.Parameters.AddWithValue("@ShipperName", data.ShipperName);
            command.Parameters.AddWithValue("@Phone", data.Phone ?? (object)DBNull.Value);

            return await command.ExecuteNonQueryAsync() > 0;
        }
    }
}