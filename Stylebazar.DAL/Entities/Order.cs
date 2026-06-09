// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — OrderStatus enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SİFARİŞ entity-si — "Orders" cədvəli. Alıcının saytdaxili alışı.
// Çatdırılma ünvanı SNAPSHOT kimi saxlanır (ünvan sonradan dəyişsə/silinsə sifariş dəqiq qalsın).
public class Order : Entity<int>
{
    // Alıcı — xarici açar (FK → AppUser). Məcburidir.
    public required string BuyerId { get; set; }
    // Sifariş statusu (Pending/Processing/Shipped/Delivered/Cancelled). Default = Pending.
    public OrderStatus Status { get; set; } = OrderStatus.Pending;
    // Ödəniş alınıb? Stripe uğurlu olanda true.
    public bool IsPaid { get; set; }
    // Stripe sessiya/ödəniş id-si — idempotentlik (eyni ödəniş iki dəfə tətbiq olunmasın) + audit.
    public string? PaymentReference { get; set; }
    // Cəmi məbləğ (ödəniş anındakı qiymətlərlə hesablanır).
    public decimal TotalAmount { get; set; }

    // --- Çatdırılma ünvanı (snapshot — sifariş anındakı dəyərlər) ---
    // Alıcının ad-soyadı (o an). Məcburidir.
    public required string ShipFullName { get; set; }
    // Telefon (o an). Məcburidir.
    public required string ShipPhone { get; set; }
    // Şəhər (o an). Məcburidir.
    public required string ShipCity { get; set; }
    // Rayon (o an). Opsionaldır.
    public string? ShipDistrict { get; set; }
    // Küçə/ev/mənzil (o an). Məcburidir.
    public required string ShipAddressLine { get; set; }
    // Poçt indeksi (o an). Opsionaldır.
    public string? ShipPostalCode { get; set; }
    // Çatdırılma qeydi (o an). Opsionaldır.
    public string? ShipNotes { get; set; }

    // --- Status izləmə tarixləri (zaman xəttini qidalandırır) ---
    // Ödənişin alındığı an. null = hələ ödənilməyib.
    public DateTime? PaidAt { get; set; }
    // Göndərildiyi an. null = göndərilməyib.
    public DateTime? ShippedAt { get; set; }
    // Çatdırıldığı an. null = çatdırılmayıb.
    public DateTime? DeliveredAt { get; set; }
    // Ləğv edildiyi an. null = ləğv olunmayıb.
    public DateTime? CancelledAt { get; set; }

    // Sifarişi verən alıcı.
    public virtual AppUser Buyer { get; set; } = null!;
    // Sifarişdəki məhsul sətirləri (bir sifariş → çox sətir).
    public virtual ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
