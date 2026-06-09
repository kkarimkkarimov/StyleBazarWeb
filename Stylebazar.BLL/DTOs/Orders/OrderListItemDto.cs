using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Orders;

// OrderListItemDto — sifariş tarixçəsi siyahısının bir sətri (yığcam görünüş).
public class OrderListItemDto
{
    public int Id { get; set; }
    public DateTime CreatedDate { get; set; }
    public OrderStatus Status { get; set; }
    public bool IsPaid { get; set; }
    public decimal TotalAmount { get; set; }
    public int TotalQuantity { get; set; }     // ümumi məhsul sayı (cəm)
    public int ItemCount { get; set; }         // fərqli sətir sayı
    public string? FirstImageUrl { get; set; } // siyahıda göstərmək üçün ilk məhsul şəkli
    public string? BuyerName { get; set; }     // satıcı/admin siyahısında alıcının adı (alıcının öz siyahısında null)
}
