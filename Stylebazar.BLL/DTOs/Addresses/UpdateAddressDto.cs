namespace Stylebazar.BLL.DTOs.Addresses;

public class UpdateAddressDto
{
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
}
