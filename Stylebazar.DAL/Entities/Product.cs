// Core.Persistence.Repositories ad fəzası — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Stylebazar.DAL.Entities.Enums — bu faylda işlənən ProductCondition enum-unu gətirir.
using Stylebazar.DAL.Entities.Enums;

// Bu sinfin yerləşdiyi ad fəzası — bütün verilənlər bazası entity-ləri burada toplanır.
namespace Stylebazar.DAL.Entities;

// MƏHSUL entity-si — bazadakı "Products" cədvəlinin BİR sətrini təmsil edir.
// Entity<int>-dən miras alır → Id (int), CreatedDate, UpdatedDate, DeletedDate (soft-delete) hazır gəlir.
public class Product : Entity<int>
{
    // Məhsulun adı. "required" → obyekt yaradılanda mütləq doldurulmalıdır (boş qala bilməz).
    public required string Name { get; set; }
    // URL üçün "təmiz" ad (slug). Məs: "Qırmızı köynək" → "qirmizi-koynek".
    public required string Slug { get; set; }
    // Məhsulun ətraflı təsviri. Məcburidir.
    public required string Description { get; set; }
    // Əsas qiymət. "decimal" tipi pul üçündür — yuvarlaqlaşma xətası vermir.
    public decimal Price { get; set; }
    // Endirimli qiymət. "?" → null ola bilər; null = endirim yoxdur.
    public decimal? DiscountPrice { get; set; }
    // Anbardakı say (stok). 0 → "Stokda yoxdur".
    public int StockQuantity { get; set; }
    // Brend adı. Opsionaldır (null ola bilər).
    public string? Brand { get; set; }
    // Məhsulun vəziyyəti (Yeni / İşlənmiş). Default = New.
    public ProductCondition Condition { get; set; } = ProductCondition.New;
    // Aid olduğu mağazanın id-si — xarici açar (FK), Store cədvəlinə bağlanır.
    public int StoreId { get; set; }
    // Aid olduğu kateqoriyanın id-si — xarici açar (FK), Category cədvəlinə bağlanır.
    public int CategoryId { get; set; }
    // Məhsul "öne çıxan / seçilmiş"dirmi? true → ana səhifədə xüsusi yerləşdirilir.
    public bool IsFeatured { get; set; }
    // Öne çıxma müddətinin bitmə tarixi. null = müddət təyin edilməyib.
    public DateTime? FeaturedUntil { get; set; }
    // Məhsul aktivdirmi? false → saytda görünmür (gizlədilir).
    public bool IsActive { get; set; }

    // --- Məhsulun zəngin xüsusiyyətləri (detal səhifəsindəki "Məlumatlar" panelində göstərilir) ---
    // Hamısı opsionaldır (null ola bilər) və sərbəst mətndir ki, satıcı dropdown-la məhdudlaşmasın.
    // UI-da dəyər null olanda "—" göstərilir.

    // Material: "Pambıq", "Polyester", "Yun".
    public string? Material { get; set; }
    // Naxış: "Düz", "Minimalist logo print".
    public string? Pattern { get; set; }
    // Yaxa forması: "V-neck", "Round Neck".
    public string? Neckline { get; set; }
    // Qol uzunluğu: "Qısa qol", "Uzun qol".
    public string? SleeveLength { get; set; }
    // Oturum/forma: "Slim", "Regular", "Loose".
    public string? Fit { get; set; }
    // Mövsüm: "Yaz, Yay" (vergüllə ayrılır).
    public string? Season { get; set; }
    // Stil: "Casual", "Formal", "Vintage".
    public string? Style { get; set; }
    // İstehsal ölkəsi: "Türkiyə", "Çin".
    public string? CountryOfOrigin { get; set; }

    // Satıcı bu məhsula çatdırılma təklif edirmi? true → alıcı "Çatdırılma var" nişanı görür.
    public bool HasDelivery { get; set; }

    // --- Naviqasiya sahələri (əlaqəli cədvəllər) ---
    // "virtual" → EF Core bu əlaqələri lazım olanda avtomatik yükləyə bilsin (lazy/eager loading).

    // Aid olduğu mağaza (çox məhsul → bir mağaza). "= null!" → EF dolduracaq, kompilyatoru susdurur.
    public virtual Store Store { get; set; } = null!;
    // Aid olduğu kateqoriya (çox məhsul → bir kateqoriya).
    public virtual Category Category { get; set; } = null!;
    // Məhsulun şəkilləri (bir məhsul → çox şəkil).
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    // Məhsulun mövcud ölçüləri (S, M, L ...).
    public virtual ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
    // Məhsulun mövcud rəngləri.
    public virtual ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();
    // Bu məhsulu seçilmişlərə (wishlist) əlavə edən istifadəçilərin qeydləri.
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    // Bu məhsula yazılmış rəylər.
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
