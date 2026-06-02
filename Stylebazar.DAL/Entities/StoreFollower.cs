using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

// MAĞAZA İZLƏYİCİSİ entity-si — "StoreFollowers" cədvəli (junction/əlaqə cədvəli).
// Bir alıcı (AppUser) bir mağazanı izləyir. (UserId, StoreId) üzərində unikal indeks var
// ki, eyni cüt üçün yalnız bir sətir olsun (təkrar izləmənin qarşısını alır).
public class StoreFollower : Entity<int>
{
    public required string UserId { get; set; }   // İzləyən istifadəçi (FK → AppUser)
    public int StoreId { get; set; }              // İzlənən mağaza (FK → Store)

    public virtual AppUser User { get; set; } = null!;     // İzləyən
    public virtual Store Store { get; set; } = null!;      // İzlənən mağaza
}
