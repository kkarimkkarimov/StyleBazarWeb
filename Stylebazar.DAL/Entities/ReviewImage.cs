// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// RƏY ŞƏKLİ entity-si — "ReviewImages" cədvəli. İstifadəçi rəyə şəkil əlavə edə bilər (bir rəy → çox şəkil).
public class ReviewImage : Entity<int>
{
    // Hansı rəyə aiddir — xarici açar (FK → Review).
    public int ReviewId { get; set; }
    // Şəklin URL/yolu. Məcburidir.
    public required string ImageUrl { get; set; }

    // Aid olduğu rəy.
    public virtual Review Review { get; set; } = null!;
}
