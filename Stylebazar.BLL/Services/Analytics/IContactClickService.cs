using Stylebazar.BLL.DTOs.Analytics;

namespace Stylebazar.BLL.Services.Analytics;

public interface IContactClickService
{
    /// <summary>
    /// Persist a contact-button click. Called from the AJAX endpoint
    /// invoked by setupContactClickTracking() in site.js.
    /// IP is anonymized (last octet zeroed) before storage.
    /// </summary>
    Task RecordAsync(RecordContactClickDto dto, string? userId, string? ipAddress, string? userAgent, string? referer, CancellationToken ct = default);

    /// <summary>
    /// Aggregate click stats across all stores owned by the given seller.
    /// Powers the /Seller/Analytics dashboard.
    /// </summary>
    Task<ContactClickStatsDto> GetStatsForSellerAsync(string userId, CancellationToken ct = default);
}
