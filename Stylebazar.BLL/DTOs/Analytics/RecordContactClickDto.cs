using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.DTOs.Analytics;

/// <summary>
/// Posted from the buyer's browser when they tap a contact-deep-link button
/// on a product or store page. ProductId is optional (null for store-page
/// header clicks), StoreId is mandatory (always resolvable).
/// </summary>
public class RecordContactClickDto
{
    public int? ProductId { get; set; }
    public int StoreId { get; set; }
    public ContactType ContactType { get; set; }
}
