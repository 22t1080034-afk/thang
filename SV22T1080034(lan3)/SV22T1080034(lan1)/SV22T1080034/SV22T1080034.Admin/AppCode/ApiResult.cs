namespace SV22T1080034.Admin
{
    /// <summary>
    /// Trả kết quả về cho loi gọi API
    /// </summary>
    public class ApiResult
    {
        /// <summary>
        /// Constructor 
        /// </summary>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public ApiResult(int code, string message) {
            Code = code;
            Message = message;
        }
        /// <summary>
        /// Mã kết quả (Qui uoc 1: thành công, 0: không thành công)
        /// </summary>
        public int Code { get; set; }
        /// <summary>
        /// Thông báo lỗi nếu có, ngược lại để rỗng
        /// </summary>
        public string Message { get; set; } = "";
    }
}
