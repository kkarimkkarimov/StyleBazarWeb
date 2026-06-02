using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

// MƏHSUL entity-si — "Products" cədvəlinə uyğundur. Satıcının satışa qoyduğu hər
// məhsul burada bir sətirdir. Entity<int>-dən miras alır → Id + audit tarixləri hazır gəlir.
public class Product : Entity<int>
{
    public required string Name { get; set; }          // Məhsulun adı (məcburi)
    public required string Slug { get; set; }          // URL-də işlənən təmiz ad (məs. "qirmizi-koynek")
    public required string Description { get; set; }    // Təsvir/izah (məcburi)
    public decimal Price { get; set; }                  // Qiymət
    public decimal? DiscountPrice { get; set; }         // Endirimli qiymət (varsa); null = endirim yoxdur
    public int StockQuantity { get; set; }              // Anbarda say
    public string? Brand { get; set; }                  // Brend (varsa)
    public ProductCondition Condition { get; set; } = ProductCondition.New; // Yeni / işlənmiş (default: Yeni)
    public int StoreId { get; set; }                    // Hansı mağazaya aiddir (FK → Store)
    public int CategoryId { get; set; }                 // Hansı kateqoriyaya aiddir (FK → Category)
    public bool IsFeatured { get; set; }                // "Seçilmiş/öne çıxan" məhsuldur?
    public DateTime? FeaturedUntil { get; set; }        // Öne çıxma nə vaxta qədər davam edir
    public bool IsActive { get; set; }                  // Aktivdir? (false = saytda görünmür)

    // --- Məhsulun zəngin xüsusiyyətləri (detal səhifəsində sağ paneldə göstərilir) ---
    // Hamısı opsionaldır (null ola bilər). Sərbəst mətndir ki, satıcı dropdown-la
    // məhdudlaşmasın. UI-da null olanda "—" göstərilir.

    public string? Material { get; set; }           // Material: "Pambıq", "Polyester", "Yun"
    public string? Pattern { get; set; }            // Naxış: "Düz", "Minimalist logo print"
    public string? Neckline { get; set; }           // Yaxa forması: "V-neck", "Round Neck"
    public string? SleeveLength { get; set; }       // Qol uzunluğu: "Qısa qol", "Uzun qol"
    public string? Fit { get; set; }                // Oturum: "Slim", "Regular", "Loose"
    public string? Season { get; set; }             // Mövsüm: "Yaz, Yay" (vergüllə)
    public string? Style { get; set; }              // Stil: "Casual", "Formal", "Vintage"
    public string? CountryOfOrigin { get; set; }    // İstehsal ölkəsi: "Türkiyə", "Çin"

    // Satıcı bu məhsul üçün çatdırılma təklif edir? true → alıcı yaşıl "Çatdırılma var"
    // nişanı görür, false → "Çatdırılma yoxdur".
    public bool HasDelivery { get; set; }

    // --- Naviqasiya sahələri (əlaqəli cədvəllər) ---
    // "virtual" → EF Core lazım olanda bunları avtomatik yükləyə bilsin (lazy/eager loading).
    public virtual Store Store { get; set; } = null!;                                   // Aid olduğu mağaza (1 məhsul → 1 mağaza)
    public virtual Category Category { get; set; } = null!;                              // Aid olduğu kateqoriya
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>(); // Məhsulun şəkilləri (1 → çox)
    public virtual ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();     // Mövcud ölçülər
    public virtual ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();  // Mövcud rənglər
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();        // Bu məhsulu seçilmişə əlavə edənlər
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();              // Məhsula yazılan rəylər
}
