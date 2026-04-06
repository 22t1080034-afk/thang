using Microsoft.Data.SqlClient;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.DataDictionary;

namespace SV22T1080034.DataLayers.SQLServer
{
    /// <summary>
    /// Cài đặt các phép xử lý dữ liệu liên quan đến tỉnh thành
    /// </summary>
    public class ProvinceRepository : BaseRepository, IDataDictionaryRepository<Province>
    {
        public ProvinceRepository(string connectionString) : base(connectionString)
        {
        }

        /// <summary>
        /// Lấy danh sách tỉnh thành
        /// </summary>
        public async Task<List<Province>> ListAsync()
        {
            List<Province> data = new List<Province>();

            using var connection = GetConnection();
            await connection.OpenAsync();

            var sql = @"SELECT ProvinceName FROM Provinces ORDER BY ProvinceName";

            using var command = new SqlCommand(sql, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                data.Add(new Province()
                {
                    ProvinceName = reader["ProvinceName"].ToString() ?? ""
                });
            }

            return data;
        }
    }
}