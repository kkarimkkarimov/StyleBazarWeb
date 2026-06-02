using Core.Application.Repositories;

namespace Core.Persistence.Repositories;

// Bütün entity-lərin (Product, Store, Category və s.) miras aldığı BAZA sinif.
// Hər obyektə lazım olan ortaq sahələri bir yerə toplayır ki, hər entity-də
// təkrar yazmayaq. "abstract"-dır — birbaşa nümunəsi yaradılmır, yalnız miras alınır.
public abstract class Entity<TId> : IEntity<TId>
{
    // Unikal açar (primary key).
    public TId Id { get; set; }

    // Obyektin yaradıldığı tarix (UTC). Bazaya ilk dəfə əlavə olunanda doldurulur.
    public DateTime CreatedDate { get; set; }

    // Son dəyişdirilmə tarixi. null = heç vaxt dəyişdirilməyib.
    public DateTime? UpdatedDate { get; set; }

    // "Yumşaq silmə" (soft delete) tarixi. null = silinməyib (aktivdir).
    // Sətir bazadan FİZİKİ silinmir — yalnız bu sahə doldurulur ki, məlumat itməsin
    // və lazım olsa geri qaytarıla bilsin.
    public DateTime? DeletedDate { get; set; }

    // Parametrsiz konstruktor — Id-ni default dəyərinə qoyur (EF Core üçün lazımdır).
    protected Entity()
    {
        Id = default!;
    }

    // Id verilərək yaradılma — obyekti dərhal hazır Id ilə qurmaq üçün.
    protected Entity(TId id)
    {
        Id = id;
    }
}
