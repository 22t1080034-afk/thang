namespace SV22T1080034.Shop.Models
{
    public class CartItem
    {
        // nhap 
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public string? Photo { get; set; }
        public decimal Price { get; set; }
        
        public int Quantity { get; set; }
        public string Unit { get; set; } = "";
        public decimal Total => Price * Quantity;
    }
}
