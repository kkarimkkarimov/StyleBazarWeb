using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Analytics;
using Stylebazar.DAL.Contexts;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Services.Analytics;

/// <summary>
/// Click-tracking + analytics aggregation. The recording side is a single
/// INSERT (high write rate path); the read side runs a handful of
/// GroupBy queries scoped to the seller's stores.
///
/// Direct ApplicationDbContext usage is intentional here: the analytics
/// aggregations (GroupBy with projection joining Product + Store metadata)
/// don't map cleanly to the generic IAsyncRepository contract, and adding
/// per-aggregate methods to IContactClickRepository would leak EF query
/// concerns into the abstraction. The repo is kept for symmetric inserts.
/// </summary>
public class ContactClickManager : IContactClickService
{
    private readonly IContactClickRepository _repo;
    private readonly ApplicationDbContext _db;

    public ContactClickManager(IContactClickRepository repo, ApplicationDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public async Task RecordAsync(
        RecordContactClickDto dto,
        string? userId,
        string? ipAddress,
        string? userAgent,
        string? referer,
        CancellationToken ct = default)
    {
        // Defensive: ContactType + StoreId always set. ProductId optional.
        var entity = new ContactClick
        {
            ProductId = dto.ProductId,
            StoreId = dto.StoreId,
            ContactType = dto.ContactType,
            UserId = userId,
            IpAddress = AnonymizeIp(ipAddress),
            UserAgent = Truncate(userAgent, 500),
            Referer = Truncate(referer, 500)
        };

        await _repo.AddAsync(entity, ct);
    }

    public async Task<ContactClickStatsDto> GetStatsForSellerAsync(string userId, CancellationToken ct = default)
    {
        // 1. Resolve the seller's store ids first (cheap & cached path)
        List<int> storeIds = await _db.Stores
            .AsNoTracking()
            .Where(s => s.OwnerId == userId)
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (storeIds.Count == 0)
        {
            return new ContactClickStatsDto();
        }

        DateTime now = DateTime.UtcNow;
        DateTime cutoff7 = now.AddDays(-7);
        DateTime cutoff30 = now.AddDays(-30);

        IQueryable<ContactClick> baseQuery = _db.ContactClicks
            .AsNoTracking()
            .Where(c => storeIds.Contains(c.StoreId));

        // Cardinality counts — three independent COUNT queries
        int total = await baseQuery.CountAsync(ct);
        int last7 = await baseQuery.Where(c => c.CreatedDate >= cutoff7).CountAsync(ct);
        int last30 = await baseQuery.Where(c => c.CreatedDate >= cutoff30).CountAsync(ct);

        // Breakdown by contact channel
        List<ContactTypeBreakdownDto> byType = await baseQuery
            .GroupBy(c => c.ContactType)
            .Select(g => new ContactTypeBreakdownDto
            {
                ContactType = g.Key,
                Count = g.Count()
            })
            .OrderByDescending(b => b.Count)
            .ToListAsync(ct);

        // Top 5 products — group by product id (excluding null product clicks
        // that came from the store-header buttons), join in product + store
        // metadata for the row.
        var topProductIds = await baseQuery
            .Where(c => c.ProductId != null)
            .GroupBy(c => c.ProductId!.Value)
            .Select(g => new { ProductId = g.Key, ClickCount = g.Count() })
            .OrderByDescending(x => x.ClickCount)
            .Take(5)
            .ToListAsync(ct);

        List<ProductClickRowDto> topProducts;
        if (topProductIds.Count == 0)
        {
            topProducts = new List<ProductClickRowDto>();
        }
        else
        {
            List<int> ids = topProductIds.Select(p => p.ProductId).ToList();
            var productLookup = await _db.Products
                .AsNoTracking()
                .Where(p => ids.Contains(p.Id))
                .Select(p => new
                {
                    p.Id,
                    p.Name,
                    p.Slug,
                    PrimaryImageUrl = p.Images
                        .OrderBy(i => i.DisplayOrder)
                        .Select(i => i.ImageUrl)
                        .FirstOrDefault(),
                    StoreName = p.Store.Name
                })
                .ToListAsync(ct);

            topProducts = topProductIds
                .Select(tp =>
                {
                    var meta = productLookup.FirstOrDefault(p => p.Id == tp.ProductId);
                    return new ProductClickRowDto
                    {
                        ProductId = tp.ProductId,
                        ProductName = meta?.Name ?? "—",
                        ProductSlug = meta?.Slug,
                        ProductImageUrl = meta?.PrimaryImageUrl,
                        StoreName = meta?.StoreName ?? "—",
                        ClickCount = tp.ClickCount
                    };
                })
                .ToList();
        }

        return new ContactClickStatsDto
        {
            TotalClicks = total,
            ClicksLast7Days = last7,
            ClicksLast30Days = last30,
            ByContactType = byType,
            TopProducts = topProducts
        };
    }

    /// <summary>
    /// Drops the last octet of an IPv4 address (192.168.1.42 → 192.168.1.0)
    /// or the last 80 bits of an IPv6 address. Coarse enough to be
    /// GDPR-friendly while still allowing rough "unique visitor" math.
    /// </summary>
    private static string? AnonymizeIp(string? ip)
    {
        if (string.IsNullOrWhiteSpace(ip)) return null;
        ip = ip.Trim();

        // IPv4 fast path
        int lastDot = ip.LastIndexOf('.');
        if (lastDot > 0 && !ip.Contains(':'))
        {
            return string.Concat(ip.AsSpan(0, lastDot), ".0");
        }

        // IPv6: keep first 48 bits (3 groups), zero the rest. Defensive truncate.
        if (ip.Contains(':'))
        {
            string[] groups = ip.Split(':');
            int kept = Math.Min(3, groups.Length);
            return string.Join(":", groups.Take(kept)) + "::";
        }

        return ip;
    }

    private static string? Truncate(string? value, int max)
    {
        if (string.IsNullOrEmpty(value)) return value;
        return value.Length > max ? value[..max] : value;
    }
}
