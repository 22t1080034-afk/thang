using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Security;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class EmployeeAccountRepository : BaseRepository, IUserAccountRepository
    {
        public EmployeeAccountRepository(string connectionString) : base(connectionString)
        {
        }

        public async Task<UserAccount?> Authorize(string userName, string password)
        {
            using var connection = GetConnection();

            string sql = @"SELECT 
                               EmployeeID AS UserId,
                               Email AS UserName,
                               FullName AS DisplayName,
                               Email,
                               Photo,
                               RoleNames
                           FROM Employees
                           WHERE Email=@userName
                                 AND Password=@password
                                 AND IsWorking=1";

            return await connection.QueryFirstOrDefaultAsync<UserAccount>(
                sql,
                new { userName, password }
            );
        }

        public async Task<bool> ChangePasswordAsync(string userName, string password)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE Employees
                           SET Password=@password
                           WHERE Email=@userName";

            return await connection.ExecuteAsync(sql,
                new { userName, password }) > 0;
        }
    }
}