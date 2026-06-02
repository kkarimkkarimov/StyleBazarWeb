using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// RƏY ŞƏKLİ entity-si — "ReviewImages" cədvəli. İstifadəçi rəyə şəkil əlavə edə bilər (bir rəy → bir neçə şəkil).
public class ReviewImage : Entity<int>
{
    public int ReviewId { get; set; }               // Hansı rəyə aiddir (FK → Review)
    public required string ImageUrl { get; set; }    // Şəklin URL/yolu

    public virtual Review Review { get; set; } = null!;  // Aid olduğu rəy
}
