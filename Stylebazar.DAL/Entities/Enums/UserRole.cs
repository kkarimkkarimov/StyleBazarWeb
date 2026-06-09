// Enum-ların yerləşdiyi ad fəzası.
namespace Stylebazar.DAL.Entities.Enums;

// İSTİFADƏÇİ ROLU enum-u. Hər istifadəçinin sistemdə bir rolu var.
// Rəqəm dəyərlər yazılmayıb → C# avtomatik 0,1,2 verir (Admin=0, Seller=1, Buyer=2). Sıra sabitdir.
public enum UserRole
{
    Admin,   // Administrator — saytı tam idarə edir.
    Seller,  // Satıcı — mağaza açır, məhsul əlavə edir.
    Buyer    // Alıcı — məhsul axtarır, seçilmişə əlavə edir, satıcı ilə əlaqə saxlayır.
}
