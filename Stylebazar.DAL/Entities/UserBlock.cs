// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities;

// İSTİFADƏÇİ BLOKU entity-si — "UserBlocks" cədvəli. Bir istifadəçi digərini bloklayır:
// bloklanmış tərəf ona mesaj göndərə bilmir (hər iki istiqamətdə göndərmə dayanır).
// (BlockerId, BlockedId) UNİKALDIR — eyni cüt üçün təkrar blok sətri yaranmır.
public class UserBlock : Entity<int>
{
    // Bloklayan istifadəçi — xarici açar (FK → AppUser). Məcburidir.
    public required string BlockerId { get; set; }
    // Bloklanan istifadəçi — xarici açar (FK → AppUser). Məcburidir.
    public required string BlockedId { get; set; }

    // Bloklayan istifadəçi.
    public virtual AppUser Blocker { get; set; } = null!;
    // Bloklanan istifadəçi.
    public virtual AppUser Blocked { get; set; } = null!;
}
