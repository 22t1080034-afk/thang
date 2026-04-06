using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.DomainModels.Sales
{
    /// <summary>
    /// Đầu vào tìm kiếm, phân trang đơn hàng
    /// </summary>
    public class OrderSearchInput : PaginationSearchInput
    {
        /// <summary>
        /// Mã khách hàng (0 nếu bỏ qua)
        /// </summary>
        public int CustomerID { get; set; }
        /// <summary>
        /// Trạng thái đơn hàng
        /// </summary>
        public OrderStatusEnum? Status { get; set; }
        /// <summary>
        /// Từ ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateFrom { get; set; }
        /// <summary>
        /// Đến ngày (ngày lập đơn hàng)
        /// </summary>
        public DateTime? DateTo { get; set; }
    }
}
