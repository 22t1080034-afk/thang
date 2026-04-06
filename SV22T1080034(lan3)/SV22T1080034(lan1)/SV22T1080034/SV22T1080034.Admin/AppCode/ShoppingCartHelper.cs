using SV22T1080034.DomainModels.Sales;

namespace SV22T1080034.Admin
{
    /// <summary>
    /// Lop cung cấp các hàm tiện ích/chuc nang liên quan đến giỏ hàng
    /// (Giỏ hàng luu trong session)
    /// </summary>
    public static class ShoppingCartHelper
    {
        /// <summary>
        /// Tên biến để luu giỏ hàng trong session
        /// </summary>
        private const string CART = "ShoppingCart";

        /// <summary>
        /// Lấy giỏ hàng tu session (nếu giỏ hàng chua có thì tạo giỏ hàng rỗng)
        /// </summary>
        /// <returns></returns>
        public static List<OrderDetailViewInfo> GetShoppingCart()
        {
            var cart = ApplicationContext.GetSessionData<List<OrderDetailViewInfo>>(CART);
            if (cart == null)
            {
                cart = new List<OrderDetailViewInfo>();
                ApplicationContext.SetSessionData(CART, cart);
            }
            return cart;
        }

        public static OrderDetailViewInfo? GetCartItem(int productID)
        {
            var cart = GetShoppingCart();
            var item = cart.Find(m => m.ProductID == productID);
            return item;
        }

        /// <summary>
        /// Thêm hàng vào giỏ
        /// </summary>
        /// <param name="data"></param>
        public static void AddItemToCart(OrderDetailViewInfo data)
        {
            var cart = GetShoppingCart();

            var existItem = cart.Find(m => m.ProductID == data.ProductID);
            if (existItem == null)
            {
                cart.Add(data);
            }
            else
            {
                existItem.Quantity += data.Quantity;
                existItem.SalePrice = data.SalePrice;
            }

            ApplicationContext.SetSessionData(CART, cart);
        }

        /// <summary>
        /// Cập nhật số luong và giá bán của một mat hàng trong giỏ
        /// </summary>
        /// <param name="productID"></param>
        /// <param name="quantity"></param>
        /// <param name="salePrice"></param>
        public static void UpdateCartItem(int productID, int quantity, decimal salePrice)
        {
            var cart = GetShoppingCart();
            var existItem = cart.Find(m => m.ProductID == productID);
            if (existItem != null)
            {
                existItem.Quantity = quantity;
                existItem.SalePrice = salePrice;

                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa 1 mat hàng khỏi giỏ dua vao mã hàng
        /// </summary>
        /// <param name="productID"></param>
        public static void RemoveItemFromCart(int productID)
        {
            var cart = GetShoppingCart();

            int index = cart.FindIndex(m => m.ProductID == productID);
            if (index >= 0)
            {
                cart.RemoveAt(index);
                ApplicationContext.SetSessionData(CART, cart);
            }
        }

        /// <summary>
        /// Xóa trống giỏ hàng
        /// </summary>
        public static void ClearCart()
        {
            var cart = new List<OrderDetailViewInfo>();
            ApplicationContext.SetSessionData(CART, cart);


        }
    }
}
