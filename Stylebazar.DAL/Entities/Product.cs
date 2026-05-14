using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.DAL.Entities;

public class Product : Entity<int>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public required string Description { get; set; }
    public decimal Price { get; set; }
    public decimal? DiscountPrice { get; set; }
    public int StockQuantity { get; set; }
    public string? Brand { get; set; }
    public ProductCondition Condition { get; set; } = ProductCondition.New;
    public int StoreId { get; set; }
    public int CategoryId { get; set; }
    public bool IsFeatured { get; set; }
    public DateTime? FeaturedUntil { get; set; }
    public bool IsActive { get; set; }

    // --- Rich product features (rendered in the right sidebar of /Shop/Detail) ---
    // All optional. Free-text so sellers aren't boxed in by a dropdown that
    // doesn't have their exact value — UI displays "—" when null.

    public string? Material { get; set; }           // "Pambıq", "Polyester", "Yun"
    public string? Pattern { get; set; }            // "Düz", "Çəkmə", "Minimalist logo print"
    public string? Neckline { get; set; }           // "Crew", "V-neck", "Round Neck"
    public string? SleeveLength { get; set; }       // "Qısa qol", "Uzun qol", "3/4"
    public string? Fit { get; set; }                // "Slim", "Regular", "Straight", "Loose"
    public string? Season { get; set; }             // "Yaz, Yay" (comma-separated)
    public string? Style { get; set; }              // "Casual", "Formal", "Vintage"
    public string? CountryOfOrigin { get; set; }    // "Türkiyə", "Çin"

    /// <summary>
    /// Does the seller offer delivery for this product? Buyer sees a green
    /// "Çatdırılma var" badge when true, neutral "Çatdırılma yoxdur" when false.
    /// </summary>
    public bool HasDelivery { get; set; }

    public virtual Store Store { get; set; } = null!;
    public virtual Category Category { get; set; } = null!;
    public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public virtual ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
    public virtual ICollection<ProductColor> Colors { get; set; } = new List<ProductColor>();
    public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
}
