// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// MAĞAZA İZLƏYİCİSİ entity-si — "StoreFollowers" cədvəli (junction/əlaqə cədvəli).
// Bir alıcı (AppUser) bir mağazanı izləyir. (UserId, StoreId) üzərində unikal indeks var
// ki, eyni cüt üçün yalnız bir sətir olsun (təkrar izləmənin qarşısını alır).
public class StoreFollower : Entity<int>
{
    // İzləyən istifadəçi — xarici açar (FK → AppUser). Məcburidir.
    public required string UserId { get; set; }
    // İzlənən mağaza — xarici açar (FK → Store).
    public int StoreId { get; set; }

    // İzləyən istifadəçi.
    public virtual AppUser User { get; set; } = null!;
    // İzlənən mağaza.
    public virtual Store Store { get; set; } = null!;
}
