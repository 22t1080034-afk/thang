using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Security;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class CustomerAccountRepository : BaseRepository, IUserAccountRepository
    {
        public CustomerAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = GetConnection();

            string sql = @"SELECT
                               CustomerID AS UserId,
                               Email AS UserName,
                               CustomerName AS DisplayName,
                               Email,
                               '' AS Photo,
                               'customer' AS RoleNames
                           FROM Customers
                           WHERE Email=@userName
                                 AND Password=@password
                                 AND (IsLocked = 0 OR IsLocked IS NULL)";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new { userName, password }
            );
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Customers
                           SET Password=@password
                           WHERE Email=@userName";

            return await connection.ExecuteAsync(sql,
                new { userName, password }) > 0;
        }
    }
}