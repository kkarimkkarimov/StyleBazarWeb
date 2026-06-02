namespace Stylebazar.BLL.DTOs.Addresses;

// CreateAddressDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateAddressDto
{
    public string Title { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string? District { get; set; }
    public string AddressLine { get; set; } = string.Empty;
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }

    /// <summary>If true, this address becomes the user's default (others are cleared).</summary>
    public bool IsDefault { get; set; }
}
