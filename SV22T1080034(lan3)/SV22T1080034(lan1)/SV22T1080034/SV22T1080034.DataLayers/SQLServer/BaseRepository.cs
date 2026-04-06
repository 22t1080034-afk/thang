using Microsoft.Data.SqlClient;

namespace SV22T1080034.DataLayers.SQLServer
{
    /// <summary>
    /// Lóp co sỏ cho các lóp cài đạt các phép xủ lý dũ liệu trên CSDL SQL Server
    /// </summary>
    public abstract class BaseRepository
    {
        protected string _connectionString;
        /// <summary>
        /// Ctor
        /// </summary>
        /// <param name="connectionString"></param>
        public BaseRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        protected SqlConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
