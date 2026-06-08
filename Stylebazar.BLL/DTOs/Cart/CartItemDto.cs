namespace Stylebazar.BLL.DTOs.Cart;

// CartItemDto — səbətdəki bir məhsulun UI/məntiq üçün görünüşü (cari qiymət + stok ilə).
public class CartItemDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public string? ImageUrl { get; set; }
    public decimal UnitPrice { get; set; }       // endirimli qiymət varsa o, yoxsa əsas qiymət
    public int Quantity { get; set; }
    public string? Size { get; set; }
    public string? Color { get; set; }
    public int StoreId { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int StockQuantity { get; set; }       // anbar (UI limit + "stokda azdır" üçün)
    public decimal LineTotal => UnitPrice * Quantity;
}
