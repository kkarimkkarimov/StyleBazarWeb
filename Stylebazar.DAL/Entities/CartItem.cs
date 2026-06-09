// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// SƏBƏT ELEMENTİ entity-si — "CartItems" cədvəli. İstifadəçinin səbətindəki bir məhsul sətri.
// DB-əsaslı səbət (istifadəçi başına): cihazlar arası qalır, ödənişdən sonra təmizlənir.
public class CartItem : Entity<int>
{
    // Səbətin sahibi — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // Hansı məhsul — xarici açar (FK → Product).
    public int ProductId { get; set; }
    // Say. Default = 1.
    public int Quantity { get; set; } = 1;
    // Seçilmiş ölçü (varsa). Opsionaldır.
    public string? Size { get; set; }
    // Seçilmiş rəng (varsa). Opsionaldır.
    public string? Color { get; set; }

    // Səbətin sahibi olan istifadəçi.
    public virtual AppUser User { get; set; } = null!;
    // Səbətdəki məhsul.
    public virtual Product Product { get; set; } = null!;
}
