using SV22T1080034.DomainModels.Common;

namespace SV22T1080034.Admin.Models
{
    /// <summary>
    /// ViewModel dùng để biểu diễn kết qả đầu ra khi tìm kiếm và hiển thị dũ liệu duói dạng phan trang
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class PaginationSearchViewModel<T> where T : class
    {
        /// <summary>
        /// Đàu vào tìm kiếm
        /// </summary>
        public required PaginationSearchInput Input { get; set; }
        /// <summary>
        /// Đầu ra tìm kiếm
        /// </summary>
        public required PagedResult<T> Result { get; set; } 
    }
}
