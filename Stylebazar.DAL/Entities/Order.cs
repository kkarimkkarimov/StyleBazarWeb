using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// SİFARİŞ entity-si — "Orders" cədvəli. Alıcının saytdaxili alışı.
// Çatdırılma ünvanı SNAPSHOT kimi saxlanır (ünvan sonradan dəyişsə/silinsə sifariş dəqiq qalsın).
public class Order : Entity<int>
{
    public required string BuyerId { get; set; }          // Alıcı (FK → AppUser)

    public OrderStatus Status { get; set; } = OrderStatus.Pending;  // Sifariş statusu
    public bool IsPaid { get; set; }                       // Ödəniş alınıb?
    public string? PaymentReference { get; set; }          // Stripe sessiya/ödəniş id-si (idempotentlik + audit)

    public decimal TotalAmount { get; set; }               // Cəmi məbləğ (ödəniş anındakı qiymətlərlə)

    // --- Çatdırılma ünvanı (snapshot) ---
    public required string ShipFullName { get; set; }
    public required string ShipPhone { get; set; }
    public required string ShipCity { get; set; }
    public string? ShipDistrict { get; set; }
    public required string ShipAddressLine { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipNotes { get; set; }

    // --- Status izləmə tarixləri ---
    public DateTime? PaidAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? CancelledAt { get; set; }

    public virtual AppUser Buyer { get; set; } = null!;
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
