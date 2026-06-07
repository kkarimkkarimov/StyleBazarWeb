using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// SƏBƏT ELEMENTİ entity-si — "CartItems" cədvəli. İstifadəçinin səbətindəki bir məhsul.
// DB-əsaslı səbət (istifadəçi başına): cihazlar arası qalır, ödənişdən sonra təmizlənir.
public class CartItem : Entity<int>
{
    public required string UserId { get; set; }   // Səbətin sahibi (FK → AppUser)
    public int ProductId { get; set; }            // Hansı məhsul (FK → Product)
    public int Quantity { get; set; } = 1;        // Say
    public string? Size { get; set; }             // Seçilmiş ölçü (varsa)
    public string? Color { get; set; }            // Seçilmiş rəng (varsa)

    public virtual AppUser User { get; set; } = null!;
    public virtual Product Product { get; set; } = null!;
}
