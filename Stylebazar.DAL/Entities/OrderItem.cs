// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SİFARİŞ SƏTRİ entity-si — "OrderItems" cədvəli. Sifarişdəki bir məhsul (snapshot ilə).
// Məhsulun adı/qiyməti/şəkli sifariş anındakı halı ilə saxlanır → tarixçə dəqiq qalır.
// StoreId saxlanır ki, satıcı öz sifarişlərini görə bilsin (marketplace — çox satıcılı səbət).
public class OrderItem : Entity<int>
{
    // Hansı sifarişə aiddir — xarici açar (FK → Order).
    public int OrderId { get; set; }
    // Məhsula istinad — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Satıcı mağaza — sifarişin hansı satıcıya aid olduğunu bildirir (FK → Store).
    public int StoreId { get; set; }

    // --- Snapshot sahələri (sifariş anındakı dəyərlər) ---
    // Məhsulun adı (o an). Məcburidir.
    public required string ProductName { get; set; }
    // Detal səhifəsinə keçid üçün slug (o an). Opsionaldır.
    public string? ProductSlug { get; set; }
    // Əsas şəkil (o an). Opsionaldır.
    public string? ImageUrl { get; set; }
    // Vahid qiymət (endirim nəzərə alınmış, o an).
    public decimal UnitPrice { get; set; }
    // Say.
    public int Quantity { get; set; }
    // Seçilmiş ölçü. Opsionaldır.
    public string? Size { get; set; }
    // Seçilmiş rəng. Opsionaldır.
    public string? Color { get; set; }

    // Sətir cəmi — saxlanmır, hər dəfə hesablanır (vahid qiymət × say).
    public decimal LineTotal => UnitPrice * Quantity;

    // Aid olduğu sifariş.
    public virtual Order Order { get; set; } = null!;
    // İstinad edilən məhsul.
    public virtual Product Product { get; set; } = null!;
    // Satıcı mağaza.
    public virtual Store Store { get; set; } = null!;
}
