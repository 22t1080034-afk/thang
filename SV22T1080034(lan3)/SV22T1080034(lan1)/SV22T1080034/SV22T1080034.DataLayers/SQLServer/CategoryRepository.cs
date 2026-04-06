using Dapper;
using Microsoft.Data.SqlClient;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Catalog;
using System.Data;

namespace SV22T1080034.DataLayers.SQLServer
{
    /// <summary>
    /// Lớp thực hiện các thao tác truy xuất dữ liệu bảng Categories trong SQL Server
    /// thông qua thư viện Dapper.
    /// Cài đặt interface IGenericRepository cho entity Category.
    /// </summary>
    public class CategoryRepository : IGenericRepository<Category>
    {
        private readonly string _connectionString;

        /// <summary>
        /// Khởi tạo repository với chuỗi kết nối đến SQL Server
        /// </summary>
        /// <param name="connectionString">Chuỗi kết nối CSDL</param>
        public CategoryRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        /// <summary>
        /// Mở kết nối đến cơ sở dữ liệu
        /// </summary>
        /// <returns>Đối tượng SqlConnection</returns>
        private IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        /// <summary>
        /// Truy vấn danh sách loại hàng có phân trang và tìm kiếm theo tên
        /// </summary>
        /// <param name="input">Thông tin tìm kiếm và phân trang</param>
        /// <returns>Kết quả dạng PagedResult</returns>
        public async Task<PagedResult<Category>> ListAsync(PaginationSearchInput input)
        {
            using var connection = GetConnection();

            var result = new PagedResult<Category>()
            {
                Page = input.Page,
                PageSize = input.PageSize
            };

            string search = $"%{input.SearchValue ?? ""}%";

            string countSql = @"
                                SELECT COUNT(*)
                                FROM Categories
                                WHERE CategoryName LIKE @SearchValue";

            result.RowCount = await connection.ExecuteScalarAsync<int>(
                countSql,
                new { SearchValue = search }
            );

            string sql = @"
                            SELECT *
                            FROM Categories
                            WHERE CategoryName LIKE @SearchValue
                            ORDER BY CategoryName
                            OFFSET @Offset ROWS
                            FETCH NEXT @PageSize ROWS ONLY";

            var data = await connection.QueryAsync<Category>(
                sql,
                new
                {
                    SearchValue = search,
                    Offset = (input.Page - 1) * input.PageSize,
                    PageSize = input.PageSize
                });

            result.DataItems = data.ToList();

            return result;
        }

        /// <summary>
        /// Lấy thông tin một loại hàng theo mã CategoryID
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>Đối tượng Category hoặc null nếu không tồn tại</returns>
        public async Task<Category?> GetAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT * FROM Categories WHERE CategoryID = @CategoryID";

            return await connection.QueryFirstOrDefaultAsync<Category>(sql,
                new { CategoryID = id });
        }

        /// <summary>
        /// Thêm mới một loại hàng vào CSDL
        /// </summary>
        /// <param name="data">Thông tin loại hàng</param>
        /// <returns>Mã CategoryID vừa được tạo</returns>
        public async Task<int> AddAsync(Category data)
        {
            using var connection = GetConnection();

            string sql = @"
                INSERT INTO Categories(CategoryName, Description)
                VALUES(@CategoryName, @Description);
                SELECT CAST(SCOPE_IDENTITY() AS INT);";

            return await connection.ExecuteScalarAsync<int>(sql, data);
        }

        /// <summary>
        /// Cập nhật thông tin loại hàng
        /// </summary>
        /// <param name="data">Thông tin cần cập nhật</param>
        /// <returns>True nếu cập nhật thành công</returns>
        public async Task<bool> UpdateAsync(Category data)
        {
            using var connection = GetConnection();

            string sql = @"
                UPDATE Categories
                SET CategoryName = @CategoryName,
                    Description = @Description
                WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql, data);

            return rows > 0;
        }

        /// <summary>
        /// Xóa loại hàng theo CategoryID
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu xóa thành công</returns>
        public async Task<bool> DeleteAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"DELETE FROM Categories WHERE CategoryID = @CategoryID";

            int rows = await connection.ExecuteAsync(sql,
                new { CategoryID = id });

            return rows > 0;
        }

        /// <summary>
        /// Kiểm tra loại hàng có đang được sử dụng trong bảng Products hay không
        /// </summary>
        /// <param name="id">Mã loại hàng</param>
        /// <returns>True nếu có dữ liệu liên quan</returns>
        public async Task<bool> IsUsedAsync(int id)
        {
            using var connection = GetConnection();

            string sql = @"SELECT COUNT(*) FROM Products WHERE CategoryID = @CategoryID";

            int count = await connection.ExecuteScalarAsync<int>(sql,
                new { CategoryID = id });

            return count > 0;
        }
    }
}