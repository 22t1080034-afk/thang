using Dapper;
using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Sales;

namespace SV22T1080034.DataLayers.SQLServer
{
    public class OrderRepository : BaseRepository, IOrderRepository
    {
        public OrderRepository(string connectionString) : base(connectionString) { }

        public async Task<int> AddOrderAsync(Order data, IEnumerable<OrderDetailViewInfo> details)
        {
            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // 1. Lưu thông tin đơn hàng
                string sqlOrder = @"INSERT INTO Orders(CustomerID, EmployeeID, OrderTime, Status, DeliveryProvince, DeliveryAddress)
                            VALUES(@CustomerID, @EmployeeID, @OrderTime, @Status, @DeliveryProvince, @DeliveryAddress);
                            SELECT CAST(SCOPE_IDENTITY() AS INT);";

                int orderID = await connection.ExecuteScalarAsync<int>(sqlOrder, data, transaction: transaction);

                // 2. Lưu chi tiết đơn hàng
                string sqlDetail = @"INSERT INTO OrderDetails(OrderID, ProductID, Quantity, SalePrice)
                             VALUES(@OrderID, @ProductID, @Quantity, @SalePrice)";

                foreach (var item in details)
                {
                    await connection.ExecuteAsync(sqlDetail, new
                    {
                        OrderID = orderID,
                        ProductID = item.ProductID,
                        Quantity = item.Quantity,
                        SalePrice = item.SalePrice
                    }, transaction: transaction);
                }

                // 3. Commit khi tất cả đều thành công
                transaction.Commit();
                return orderID;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return 0;
            }
        }

        public async Task<bool> DeleteAsync(int orderID)
        {
            using var connection = GetConnection();
            connection.Open();

            using var transaction = connection.BeginTransaction();

            try
            {
                // Xóa chi tiết đơn hàng trước
                await connection.ExecuteAsync(
                    "DELETE FROM OrderDetails WHERE OrderID=@orderID",
                    new { orderID },
                    transaction: transaction);

                // Xóa đơn hàng
                int rows = await connection.ExecuteAsync(
                    "DELETE FROM Orders WHERE OrderID=@orderID",
                    new { orderID },
                    transaction: transaction);

                transaction.Commit();
                return rows > 0;
            }
            catch (Exception)
            {
                transaction.Rollback();
                return false;
            }
        }

        public async Task<OrderViewInfo?> GetAsync(int orderID)
        {
            using var connection = GetConnection();

            string sql = @"
                            SELECT 
                                O.*,

                                C.CustomerName,
                                C.ContactName AS CustomerContactName,
                                C.Email AS CustomerEmail,
                                C.Phone AS CustomerPhone,
                                C.Address AS CustomerAddress,

                                E.FullName AS EmployeeName,

                                S.ShipperName,
                                S.Phone AS ShipperPhone

                            FROM Orders O
                            LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                            LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID
                            LEFT JOIN Shippers S ON O.ShipperID = S.ShipperID -- Thêm dòng này để lấy thông tin Shipper

                            WHERE O.OrderID = @orderID";

            return await connection.QueryFirstOrDefaultAsync<OrderViewInfo>(sql, new { orderID });
        }

        public async Task<bool> UpdateAsync(Order data)
        {
            using var connection = GetConnection();

            // Cập nhật ĐẦY ĐỦ các trường thông tin thay vì chỉ cập nhật mỗi Status
            string sql = @"UPDATE Orders
                   SET CustomerID = @CustomerID,
                       EmployeeID = @EmployeeID,
                       AcceptTime = @AcceptTime,
                       ShipperID = @ShipperID,
                       ShippedTime = @ShippedTime,
                       FinishedTime = @FinishedTime,
                       Status = @Status,
                       DeliveryProvince = @DeliveryProvince,
                       DeliveryAddress = @DeliveryAddress
                   WHERE OrderID = @OrderID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<PagedResult<OrderViewInfo>> ListAsync(OrderSearchInput input)
        {
            using var connection = GetConnection();

            var condition = "";
            var parameters = new DynamicParameters();

            // 🔍 Search tên khách
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                condition += " AND C.CustomerName LIKE @SearchValue";
                parameters.Add("SearchValue", $"%{input.SearchValue}%");
            }

            // 🔍 Status
            if (input.Status.HasValue)
            {
                condition += " AND O.Status = @Status";
                parameters.Add("Status", input.Status.Value);
            }

            // 🔍 From date
            if (input.DateFrom.HasValue)
            {
                condition += " AND O.OrderTime >= @FromTime";
                parameters.Add("FromTime", input.DateFrom.Value);
            }

            // 🔍 To date
            if (input.DateTo.HasValue)
            {
                condition += " AND O.OrderTime <= @ToTime";
                parameters.Add("ToTime", input.DateTo.Value);
            }

            // 🔢 COUNT
            string sqlCount;

            // 👉 nếu có search tên → cần JOIN Customers
            if (!string.IsNullOrWhiteSpace(input.SearchValue))
            {
                sqlCount = $@"
                            SELECT COUNT(*)
                            FROM Orders O
                            LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                            WHERE 1=1 {condition}";
            }
            else
            {
                // 👉 không search → KHÔNG JOIN (nhanh hơn)
                sqlCount = $@"
                            SELECT COUNT(*)
                            FROM Orders O
                            WHERE 1=1 {condition}";
            }

            int count = await connection.ExecuteScalarAsync<int>(sqlCount, parameters);

            // 📄 DATA
            string sqlData = $@"
                                SELECT 
                                    O.OrderID,
                                    O.CustomerID,
                                    O.EmployeeID,
                                    O.OrderTime,
                                    O.AcceptTime,
                                    O.Status,

                                    C.CustomerName,
                                    C.Phone AS CustomerPhone,
                                    E.FullName AS EmployeeName,

                                    ISNULL(T.TotalPrice, 0) AS TotalPrice

                                FROM Orders O
                                LEFT JOIN Customers C ON O.CustomerID = C.CustomerID
                                LEFT JOIN Employees E ON O.EmployeeID = E.EmployeeID

                                OUTER APPLY (
                                    SELECT SUM(Quantity * SalePrice) AS TotalPrice
                                    FROM OrderDetails
                                    WHERE OrderID = O.OrderID
                                ) T

                                WHERE 1=1 {condition}

                                ORDER BY O.OrderTime DESC
                                OFFSET @offset ROWS FETCH NEXT @pagesize ROWS ONLY";

            parameters.Add("offset", (input.Page - 1) * input.PageSize);
            parameters.Add("pagesize", input.PageSize);

            var data = await connection.QueryAsync<OrderViewInfo>(sqlData, parameters);

            return new PagedResult<OrderViewInfo>()
            {
                Page = input.Page,
                PageSize = input.PageSize,
                RowCount = count,
                DataItems = data.ToList()
            };
        }

        // ORDER DETAIL

        public async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            using var connection = GetConnection();

            // Dùng JOIN để lấy tên sản phẩm, đơn vị tính và ảnh từ bảng Products
            string sql = @"
                SELECT 
                    OD.OrderID, 
                    OD.ProductID, 
                    OD.Quantity, 
                    OD.SalePrice,
                    P.ProductName, 
                    P.Unit, 
                    P.Photo
                FROM OrderDetails OD
                JOIN Products P ON OD.ProductID = P.ProductID
                WHERE OD.OrderID = @orderID";

            var data = await connection.QueryAsync<OrderDetailViewInfo>(sql, new { orderID });

            return data.ToList();
        }

        public async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();

            // Tương tự, dùng JOIN để lấy thông tin hiển thị
            string sql = @"
                SELECT 
                    OD.OrderID, 
                    OD.ProductID, 
                    OD.Quantity, 
                    OD.SalePrice,
                    P.ProductName, 
                    P.Unit, 
                    P.Photo
                FROM OrderDetails OD
                JOIN Products P ON OD.ProductID = P.ProductID
                WHERE OD.OrderID = @orderID AND OD.ProductID = @productID";

            return await connection.QueryFirstOrDefaultAsync<OrderDetailViewInfo>(sql, new { orderID, productID });
        }

        public async Task<bool> AddDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();

            string sql = @"INSERT INTO OrderDetails(OrderID,ProductID,Quantity,SalePrice)
                           VALUES(@OrderID,@ProductID,@Quantity,@SalePrice)";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            using var connection = GetConnection();

            string sql = @"UPDATE OrderDetails
                           SET Quantity=@Quantity,
                               SalePrice=@SalePrice
                           WHERE OrderID=@OrderID AND ProductID=@ProductID";

            return await connection.ExecuteAsync(sql, data) > 0;
        }

        public async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            using var connection = GetConnection();

            string sql = @"DELETE FROM OrderDetails
                           WHERE OrderID=@orderID AND ProductID=@productID";

            return await connection.ExecuteAsync(sql,
                new { orderID, productID }) > 0;
        }
    }
}