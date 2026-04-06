using SV22T1080034.DataLayers.Interfaces;
using SV22T1080034.DataLayers.SQLServer;
using SV22T1080034.DomainModels.Common;
using SV22T1080034.DomainModels.Sales;

namespace SV22T1080034.BusinessLayers
{
    /// <summary>
    /// Cung cấp các chức năng xử lý dữ liệu liên quan đến bán hàng
    /// bao gồm: đơn hàng (Order) và chi tiết đơn hàng (OrderDetail).
    /// </summary>
    public static class SalesDataService
    {
        private static readonly IOrderRepository orderDB;

        /// <summary>
        /// Constructor
        /// </summary>
        static SalesDataService()
        {
            orderDB = new OrderRepository(Configuration.ConnectionString);
        }

        #region Order

        /// <summary>
        /// Tìm kiếm và lấy danh sách đơn hàng dưới dạng phân trang
        /// </summary>
        public static async Task<PagedResult<OrderViewInfo>> ListOrdersAsync(OrderSearchInput input)
        {
            return await orderDB.ListAsync(input);
        }

        /// <summary>
        /// Lấy thông tin chi tiết của một đơn hàng
        /// </summary>
        public static async Task<OrderViewInfo?> GetOrderAsync(int orderID)
        {
            return await orderDB.GetAsync(orderID);
        }

        /// <summary>
        /// Thay bang chuc nang này
        /// </summary>
        /// <param name="customerID"></param>
        /// <param name="deliveryProvince"></param>
        /// <param name="deliverydAdress"></param>
        /// <returns></returns>
        /// <summary>
        /// Lập đơn hàng mới (Trạng thái New, các thông tin xử lý đều rỗng)
        /// </summary>
        public static async Task<int> AddOrderAsync(int? customerID, string deliveryProvince, string deliveryAddress, IEnumerable<OrderDetailViewInfo> cartItems)
        {
            // Nếu giỏ hàng trống thì không cho lập đơn
            if (cartItems == null || !cartItems.Any()) return 0;

            var order = new Order
            {
                CustomerID = customerID,
                DeliveryProvince = deliveryProvince ?? "",
                DeliveryAddress = deliveryAddress ?? "",
                OrderTime = DateTime.Now,
                Status = OrderStatusEnum.New,

                // Đảm bảo các thông tin này bằng NULL khi đơn hàng mới được tạo
                EmployeeID = null,
                AcceptTime = null,
                ShipperID = null,
                ShippedTime = null,
                FinishedTime = null
            };

            // Gọi repository xử lý bằng Transaction
            return await orderDB.AddOrderAsync(order, cartItems);
        }

        /// <summary>
        /// Cập nhật thông tin đơn hàng
        /// </summary>
        public static async Task<bool> UpdateOrderAsync(Order data)
        {
            //Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật
            // 1. Kiểm tra ID hợp lệ
            if (data.OrderID <= 0) return false;

            // 2. Lấy đơn hàng hiện tại từ DB để kiểm tra trạng thái
            var existingOrder = await orderDB.GetAsync(data.OrderID);
            if (existingOrder == null) return false;

            // 3. Chỉ cho phép cập nhật thông tin (địa chỉ, tỉnh thành...) khi đơn hàng ở trạng thái Mới (New) hoặc Đã duyệt (Accepted)
            if (existingOrder.Status != OrderStatusEnum.New && existingOrder.Status != OrderStatusEnum.Accepted)
            {
                return false;
            }

            return await orderDB.UpdateAsync(data);
        }

        /// <summary>
        /// Xóa đơn hàng
        /// </summary>
        public static async Task<bool> DeleteOrderAsync(int orderID)
        {
            //Kiểm tra trạng thái đơn hàng trước khi xóa
            if (orderID <= 0) return false;

            var existingOrder = await orderDB.GetAsync(orderID);
            if (existingOrder == null) return false;

            // Chỉ cho phép xóa vật lý (khỏi Database) khi đơn hàng mới lập (New), bị hủy (Cancelled) hoặc bị từ chối (Rejected)
            // KHÔNG được xóa đơn đang giao (Shipping) hoặc đã hoàn tất (Completed) để giữ lịch sử doanh thu
            if (existingOrder.Status == OrderStatusEnum.Accepted ||
                existingOrder.Status == OrderStatusEnum.Shipping ||
                existingOrder.Status == OrderStatusEnum.Completed)
            {
                return false;
            }

            return await orderDB.DeleteAsync(orderID);
        }

        #endregion

        #region Order Status Processing

        /// <summary>
        /// Duyệt đơn hàng
        /// </summary>
        public static async Task<bool> AcceptOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.New)
                return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now;
            order.Status = OrderStatusEnum.Accepted;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Từ chối đơn hàng
        /// </summary>
        public static async Task<bool> RejectOrderAsync(int orderID, int employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null || order.Status != OrderStatusEnum.New) return false;

            order.EmployeeID = employeeID;
            order.AcceptTime = DateTime.Now; // Ghi nhận luôn thời gian xử lý để UI không bị trống
            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Rejected;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hủy đơn hàng
        /// </summary>
        /// <param name="orderID">Mã đơn hàng</param>
        /// <param name="employeeID">ID nhân viên xử lý (có thể null nếu khách hàng tự hủy)</param>
        public static async Task<bool> CancelOrderAsync(int orderID, int? employeeID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;

            if (order.Status != OrderStatusEnum.New &&
                order.Status != OrderStatusEnum.Accepted &&
                order.Status != OrderStatusEnum.Shipping)
                return false;

            // Nếu đơn hàng chưa từng có ai duyệt (hủy ngay lúc Mới), ghi nhận người xử lý (nếu có)
            if (order.EmployeeID == null && employeeID.HasValue)
            {
                order.EmployeeID = employeeID.Value;
                order.AcceptTime = DateTime.Now;
            }

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Cancelled;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Giao đơn hàng cho người giao hàng
        /// </summary>
        public static async Task<bool> ShipOrderAsync(int orderID, int shipperID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Accepted)
                return false;

            order.ShipperID = shipperID;
            order.ShippedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Shipping;

            return await orderDB.UpdateAsync(order);
        }

        /// <summary>
        /// Hoàn tất đơn hàng
        /// </summary>
        public static async Task<bool> CompleteOrderAsync(int orderID)
        {
            var order = await orderDB.GetAsync(orderID);
            if (order == null)
                return false;

            if (order.Status != OrderStatusEnum.Shipping)
                return false;

            order.FinishedTime = DateTime.Now;
            order.Status = OrderStatusEnum.Completed;

            return await orderDB.UpdateAsync(order);
        }

        #endregion

        #region Order Detail

        /// <summary>
        /// Lấy danh sách mặt hàng của đơn hàng
        /// </summary>
        public static async Task<List<OrderDetailViewInfo>> ListDetailsAsync(int orderID)
        {
            return await orderDB.ListDetailsAsync(orderID);
        }

        /// <summary>
        /// Lấy thông tin một mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<OrderDetailViewInfo?> GetDetailAsync(int orderID, int productID)
        {
            return await orderDB.GetDetailAsync(orderID, productID);
        }

        /// <summary>
        /// Thêm mặt hàng vào đơn hàng
        /// </summary>
        public static async Task<bool> AddDetailAsync(OrderDetail data)
        {
            //Kiểm tra dữ liệu và trạng thái đơn hàng trước khi thêm mặt hàng
            // 1. Kiểm tra dữ liệu đầu vào cơ bản
            if (data.OrderID <= 0 || data.ProductID <= 0) return false;
            if (data.Quantity <= 0) return false;
            if (data.SalePrice < 0) return false;

            // 2. Kiểm tra trạng thái đơn hàng
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null) return false;

            // Chỉ cho phép thêm mặt hàng khi đơn hàng ở trạng thái Mới (New) hoặc Đã duyệt (Accepted)
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                return false;
            }

            return await orderDB.AddDetailAsync(data);
        }

        /// <summary>
        /// Cập nhật mặt hàng trong đơn hàng
        /// </summary>
        public static async Task<bool> UpdateDetailAsync(OrderDetail data)
        {
            //Kiểm tra dữ liệu và trạng thái đơn hàng trước khi cập nhật mặt hàng
            // 1. Kiểm tra dữ liệu đầu vào
            if (data.OrderID <= 0 || data.ProductID <= 0) return false;
            if (data.Quantity <= 0) return false;
            if (data.SalePrice < 0) return false;

            // 2. Kiểm tra trạng thái đơn hàng
            var order = await orderDB.GetAsync(data.OrderID);
            if (order == null) return false;

            // Chỉ cho phép sửa mặt hàng khi đơn hàng ở trạng thái Mới (New) hoặc Đã duyệt (Accepted)
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                return false;
            }

            return await orderDB.UpdateDetailAsync(data);
        }

        /// <summary>
        /// Xóa mặt hàng khỏi đơn hàng
        /// </summary>
        public static async Task<bool> DeleteDetailAsync(int orderID, int productID)
        {
            // Kiểm tra trạng thái đơn hàng trước khi xóa mặt hàng
            // 1. Kiểm tra mã hợp lệ
            if (orderID <= 0 || productID <= 0) return false;

            // 2. Kiểm tra trạng thái đơn hàng
            var order = await orderDB.GetAsync(orderID);
            if (order == null) return false;

            // Chỉ cho phép xóa mặt hàng khi đơn hàng ở trạng thái Mới (New) hoặc Đã duyệt (Accepted)
            if (order.Status != OrderStatusEnum.New && order.Status != OrderStatusEnum.Accepted)
            {
                return false;
            }

            return await orderDB.DeleteDetailAsync(orderID, productID);
        }

        #endregion
    }
}