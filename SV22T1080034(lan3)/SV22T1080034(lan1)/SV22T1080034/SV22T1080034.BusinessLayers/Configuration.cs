namespace SV22T1080034.BusinessLayers
{
    /// <summary>
    /// Khoi tao và luu tru các thông tin cấu hình sur dụng cho Business Layer
    /// </summary>
    public static class Configuration
    {
        private static string _connectionString = string.Empty;
        /// <summary>
        /// Khoi tạo cấu hình cho Business Layer (Hàm này phải duoc gọi truoc khi chạy úng dụng)
        /// </summary>
        /// <param name="connectionString">Chuôix thàm ố kết nối đến CSDL</param>
        public static void Initialize(string connectionString)
        {
            _connectionString = connectionString;
        }
         public static string ConnectionString => _connectionString;
    }
}
