using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// SİFARİŞ SƏTRİ entity-si — "OrderItems" cədvəli. Sifarişdəki bir məhsul (snapshot ilə).
// Məhsulun adı/qiyməti/şəkli sifariş anındakı halı ilə saxlanır → tarixçə dəqiq qalır.
// StoreId saxlanır ki, satıcı öz sifarişlərini görə bilsin (marketplace — çox satıcılı səbət).
public class OrderItem : Entity<int>
{
    public int OrderId { get; set; }                   // Hansı sifarişə aiddir (FK → Order)
    public int ProductId { get; set; }                 // Məhsula istinad (FK → Product)
    public int StoreId { get; set; }                   // Satıcı mağaza (sifarişin hansı satıcıya aid olduğu)

    // --- Snapshot sahələri (sifariş anındakı dəyərlər) ---
    public required string ProductName { get; set; }   // Məhsulun adı (o an)
    public string? ProductSlug { get; set; }           // Detal səhifəsinə keçid üçün
    public string? ImageUrl { get; set; }              // Əsas şəkil (o an)
    public decimal UnitPrice { get; set; }             // Vahid qiymət (endirim nəzərə alınmış, o an)
    public int Quantity { get; set; }                  // Say
    public string? Size { get; set; }                  // Seçilmiş ölçü
    public string? Color { get; set; }                 // Seçilmiş rəng

    public decimal LineTotal => UnitPrice * Quantity;  // Sətir cəmi (hesablanır)

    public virtual Order Order { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
    public virtual Store Store { get; set; } = null!;
}
