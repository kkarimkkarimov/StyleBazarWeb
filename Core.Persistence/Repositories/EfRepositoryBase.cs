using System.Linq.Expressions;
using Core.Persistence.Paging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace Core.Persistence.Repositories;

// Repository pattern-in ƏSL gerçəkləşməsi (implementation).
// IAsyncRepository interfeysindəki bütün metodları Entity Framework Core ilə həyata
// keçirir. Hər konkret repository (məs. ProductRepository) bundan miras alır və
// CRUD kodunu TƏKRAR yazmır — hamısı burada bir dəfə yazılıb.
//   TEntity   — entity tipi,  TEntityId — onun Id tipi,  TContext — DbContext tipi
public class EfRepositoryBase<TEntity, TEntityId, TContext> : IAsyncRepository<TEntity, TEntityId>
    where TEntity : Entity<TEntityId>   // entity bizim baza Entity sinfindən olmalıdır
    where TContext : DbContext          // context EF Core DbContext olmalıdır
{
    // Verilənlər bazası ilə əlaqə obyekti. DI (asılılıq inyeksiyası) ilə gəlir.
    protected readonly TContext Context;

    public EfRepositoryBase(TContext context)
    {
        Context = context;
    }

    // ── BİR obyekt gətir ──────────────────────────────────────────────
    public async Task<TEntity?> GetAsync(
        Expression<Func<TEntity, bool>> predicate,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        // Set<TEntity>() — həmin entity-nin cədvəlinə sorğu başlanğıcı.
        IQueryable<TEntity> queryable = Context.Set<TEntity>();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();   // izləmə yoxdursa — daha sürətli, yalnız oxumaq üçün
        if (include != null)
            queryable = include(queryable);          // əlaqəli cədvəlləri qoş (məs. .Include(p => p.Images))
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters(); // silinmişləri də gətir (soft-delete filtrini söndür)

        // Şərtə uyğun İLK obyekti qaytar; tapılmasa null.
        return await queryable.FirstOrDefaultAsync(predicate, cancellationToken);
    }

    // ── SİYAHI gətir (səhifələnmiş) ───────────────────────────────────
    public async Task<Paginate<TEntity>> GetListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
        Func<IQueryable<TEntity>, IIncludableQueryable<TEntity, object>>? include = null,
        int index = 0,
        int size = 10,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Context.Set<TEntity>();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (include != null)
            queryable = include(queryable);
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);  // axtarış şərtini tətbiq et

        // Əvvəlcə ÜMUMİ sayı tapırıq (neçə səhifə olacağını hesablamaq üçün).
        int count = await queryable.CountAsync(cancellationToken);
        // Sıralama varsa tətbiq et.
        IQueryable<TEntity> ordered = orderBy != null ? orderBy(queryable) : queryable;
        // Skip/Take — yalnız istənilən səhifənin elementlərini götür:
        //   Skip(index*size) əvvəlki səhifələri ötür, Take(size) bu səhifəni götürür.
        List<TEntity> items = await ordered.Skip(index * size).Take(size).ToListAsync(cancellationToken);

        return new Paginate<TEntity>
        {
            Index = index,
            Size = size,
            Count = count,
            // Ümumi səhifə sayı = ümumi say / səhifə ölçüsü, yuxarı yuvarlaqlaşdırılmış.
            Pages = size > 0 ? (int)Math.Ceiling(count / (double)size) : 0,
            Items = items
        };
    }

    // ── Mövcudluq yoxlaması (varmı/yoxmu) ─────────────────────────────
    public async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        bool withDeleted = false,
        bool enableTracking = true,
        CancellationToken cancellationToken = default)
    {
        IQueryable<TEntity> queryable = Context.Set<TEntity>();
        if (!enableTracking)
            queryable = queryable.AsNoTracking();
        if (withDeleted)
            queryable = queryable.IgnoreQueryFilters();
        if (predicate != null)
            queryable = queryable.Where(predicate);

        // Şərtə uyğun ən azı bir sətir varsa true (bütün sətirləri gətirmir — sürətli).
        return await queryable.AnyAsync(cancellationToken);
    }

    // ── ƏLAVƏ et (INSERT) ─────────────────────────────────────────────
    public async Task<TEntity> AddAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddAsync(entity, cancellationToken); // yaddaşda əlavə kimi qeyd et
        await Context.SaveChangesAsync(cancellationToken);                 // bazaya YAZ (real INSERT burada baş verir)
        return entity;
    }

    // ── Toplu ƏLAVƏ (bir neçəsini birdən) ─────────────────────────────
    public async Task<ICollection<TEntity>> AddRangeAsync(
        ICollection<TEntity> entities,
        CancellationToken cancellationToken = default)
    {
        await Context.Set<TEntity>().AddRangeAsync(entities, cancellationToken);
        await Context.SaveChangesAsync(cancellationToken);
        return entities;
    }

    // ── YENİLƏ (UPDATE) ───────────────────────────────────────────────
    public async Task<TEntity> UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        Context.Set<TEntity>().Update(entity);             // obyekti "dəyişdi" kimi işarələ
        await Context.SaveChangesAsync(cancellationToken); // bazaya yaz
        return entity;
    }

    // ── SİL (yumşaq və ya fiziki) ─────────────────────────────────────
    public async Task<TEntity> DeleteAsync(
        TEntity entity,
        bool permanent = false,
        CancellationToken cancellationToken = default)
    {
        if (permanent)
        {
            // Fiziki silmə — sətir bazadan tamamilə silinir.
            Context.Set<TEntity>().Remove(entity);
        }
        else
        {
            // Yumşaq silmə — sətir qalır, yalnız silinmə tarixi qoyulur.
            // Global query filter bundan sonra bu sətri avtomatik gizlədir.
            entity.DeletedDate = DateTime.UtcNow;
            Context.Set<TEntity>().Update(entity);
        }

        await Context.SaveChangesAsync(cancellationToken);
        return entity;
    }
}
