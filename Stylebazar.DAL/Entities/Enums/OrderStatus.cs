// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// SİFARİŞ STATUSU enum-u — sifarişin həyat dövrünü izləyir.
// Alıcı bunu görür (zaman xətti); satıcı/admin irəli aparır.
public enum OrderStatus
{
    Pending = 0,     // Sifariş verilib, ödəniş gözlənilir.
    Processing = 1,  // Ödəniş alınıb, hazırlanır.
    Shipped = 2,     // Göndərilib (yolda).
    Delivered = 3,   // Çatdırılıb.
    Cancelled = 4    // Ləğv edilib.
}
