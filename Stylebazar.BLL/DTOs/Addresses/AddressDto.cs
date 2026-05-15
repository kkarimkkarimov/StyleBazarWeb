namespace Stylebazar.BLL.DTOs.Addresses;

/// <summary>
/// Read-side address DTO. Shipped to both the Account → Addresses page and
/// Seller → Profile address card.
/// </summary>
public class AddressDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
}
