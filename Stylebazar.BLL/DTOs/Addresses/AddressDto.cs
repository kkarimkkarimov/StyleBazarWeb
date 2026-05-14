namespace Stylebazar.BLL.DTOs.Addresses;

public class AddressDto
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? PostalCode { get; set; }
    public string? Notes { get; set; }
    public bool IsDefault { get; set; }
    public DateTime CreatedDate { get; set; }
}
