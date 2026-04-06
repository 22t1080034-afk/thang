using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Partner;

namespace SV22T1080034.BusinessLayers
{
    /// <summary>
    /// Các chức năng dữ liệu chung của hệ thống
    /// </summary>
    public static class CommonDataService
    {
        // Khai báo một interface IGenericRepository để gọi các hàm trong DataLayer
        private static readonly IGenericRepository<Shipper> shipperDB;

        /// <summary>
        /// Constructor tĩnh này dùng để khởi tạo shipperDB (gọi đến ShipperRepository)
        /// Thường thì chuỗi kết nối (connectionString) sẽ được cấu hình lúc chạy app (trong Program.cs)
        /// </summary>
        static CommonDataService()
        {
            // Chú ý: Cậu cần có lớp cấu hình Configuration để lấy chuỗi kết nối
            string connectionString = Configuration.ConnectionString;
            shipperDB = new SV22T1080034.DataLayers.SQLServer.ShipperRepository(connectionString);
        }

        public static PaginationSearchInput CreateSearchInput(
            int page,
            int pageSize,
            string searchValue)
        {
            return new PaginationSearchInput()
            {
                Page = page,
                PageSize = pageSize,
                SearchValue = searchValue ?? ""
            };
        }

        /// <summary>
        /// Lấy danh sách tất cả những người giao hàng (không phân trang, hoặc phân trang rất lớn)
        /// </summary>
        /// <returns></returns>
        public static async Task<List<Shipper>> ListOfShippersAsync()
        {
            // Để lấy tất cả, ta truyền vào pageSize lớn (vd: 1000)
            var input = CreateSearchInput(1, 1000, "");
            var result = await shipperDB.ListAsync(input);
            return result.DataItems;
        }
    }
}