using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Analytics;

/// <summary>
/// Aggregated click statistics for a single seller. Powers the
/// /Seller/Analytics dashboard cards.
/// </summary>
public class ContactClickStatsDto
{
    /// <summary>Total clicks across all stores + products + types.</summary>
    public int TotalClicks { get; set; }

    /// <summary>Clicks recorded in the trailing 7 days.</summary>
    public int ClicksLast7Days { get; set; }

    /// <summary>Clicks recorded in the trailing 30 days.</summary>
    public int ClicksLast30Days { get; set; }

    /// <summary>Click counts split by channel (WhatsApp / Instagram / Phone / …).</summary>
    public List<ContactTypeBreakdownDto> ByContactType { get; set; } = new();

    /// <summary>Top N products by click count (default top 5).</summary>
    public List<ProductClickRowDto> TopProducts { get; set; } = new();
}

public class ContactTypeBreakdownDto
{
    public ContactType ContactType { get; set; }
    public int Count { get; set; }
}

public class ProductClickRowDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductSlug { get; set; }
    public string? ProductImageUrl { get; set; }
    public string StoreName { get; set; } = string.Empty;
    public int ClickCount { get; set; }
}
