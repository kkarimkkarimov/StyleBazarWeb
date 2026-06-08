namespace Stylebazar.BLL.DTOs.Orders;

// PlaceOrderDto — checkout formundan gələn sifariş məlumatı.
// İki rejim: (1) mövcud ünvan seçilib (AddressId), (2) yeni ünvan sahələri doldurulub.
public class PlaceOrderDto
{
    public int? AddressId { get; set; }          // mövcud ünvan seçilibsə onun id-si

    // Yeni ünvan sahələri (AddressId boşdursa istifadə olunur)
    public string? ShipFullName { get; set; }
    public string? ShipPhone { get; set; }
    public string? ShipCity { get; set; }
    public string? ShipDistrict { get; set; }
    public string? ShipAddressLine { get; set; }
    public string? ShipPostalCode { get; set; }
    public string? ShipNotes { get; set; }

    public bool SaveAddress { get; set; }        // yeni ünvanı ünvan kitabçasına saxla
}
