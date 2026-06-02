using System.Linq.Expressions;
using Core.Application.Repositories;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore.Query;

namespace Core.Persistence.Repositories;

// Generic (universal) repository müqaviləsi (interfeys).
// Bütün entity-lər üçün eyni CRUD əməliyyatlarını (oxu / əlavə / dəyiş / sil) BİR
// dəfə təyin edirik ki, hər repository-də təkrar yazmayaq.
//   TEntity   — hansı entity ilə işləyirik
//   TEntityId — həmin entity-nin Id sahəsinin tipi
public interface IAsyncRepository<TEntity, TEntityId> : IRepository
    where TEntity : class, IEntity<TEntityId>   // TEntity mütləq sinif və IEntity olmalıdır
{
    // BİR ədəd obyekt gətirir (şərtə uyğun ilk tapılan).
    //   predicate      — axtarış şərti (məs. x => x.Id == 5)
    //   include        — əlaqəli cədvəlləri də gətir (məs. məhsulun şəkilləri)
    //   withDeleted    — silinmişləri də daxil et? (default: yox)
    //   enableTracking — EF dəyişiklikləri izləsin? (yalnız oxuyuruqsa false daha sürətlidir)
    Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default);

    // SİYAHI gətirir — səhifələnmiş (paging) şəkildə (məs. 1000 məhsuldan 10-u).
    //   orderBy — sıralama (məs. qiymətə görə)
    //   index   — neçənci səhifə (0-dan başlayır), size — səhifədə neçə element
    Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default);

    // Şərtə uyğun ƏN AZI BİR obyekt varmı? true/false qaytarır (mövcudluq yoxlaması).
    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default);

    // Yeni obyekt ƏLAVƏ edir (verilənlər bazasına INSERT).
    Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default);

    // Bir neçə obyekti birdən əlavə edir (toplu INSERT).
    Task<ICollection<TEntity>> AddRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default);

    // Mövcud obyekti YENİLƏYİR (UPDATE).
    Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    // Obyekti SİLİR.
    //   permanent = false → yumşaq silmə (DeletedDate doldurulur, məlumat qalır)
    //   permanent = true  → fiziki silmə (bazadan tamamilə silinir)
    Task<TEntity> DeleteAsync(
        TEntity entity,
        bool permanent = false,
        CancellationToken cancellationToken = default);
}
