using Stylebazar.BLL.DTOs.Analytics;

namespace Stylebazar.BLL.Services.Analytics;

// ISellerAnalyticsService — bu xidmətin MÜQAVİLƏSİ (interfeys). Hansı əməliyyatların mövcud olacağını təyin edir; gerçəkləşmə ayrı sinifdədir.
public interface ISellerAnalyticsService
{
    /// <summary>
    /// One-shot aggregate for the seller dashboard. Resolves stores owned by
    /// the current user, then aggregates product / wishlist / review /
    /// click counts across them.
    /// </summary>
    Task<SellerAnalyticsDto> GetForCurrentSellerAsync(CancellationToken ct = default);
}
