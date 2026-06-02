namespace Stylebazar.DAL.Entities.Enums;

// İSTİFADƏÇİ ROLU enum-u. Hər istifadəçinin sistemdə bir rolu var.
// (Rəqəm dəyərlər: Admin=0, Seller=1, Buyer=2 — sıra sabitdir.)
public enum UserRole
{
    Admin,   // Administrator — saytı tam idarə edir
    Seller,  // Satıcı — mağaza açır, məhsul əlavə edir
    Buyer    // Alıcı — məhsul axtarır, seçilmişə əlavə edir, satıcı ilə əlaqə saxlayır
}
