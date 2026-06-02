using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;

namespace Stylebazar.MVC.ViewModels;

// ShopDetailViewModel — view (səhifə) üçün məlumat modeli. Controller doldurur, Razor səhifəsi göstərir.
public class ShopDetailViewModel
{
    public ProductDetailDto Product { get; set; } = new();
    public Paginate<ReviewDto> Reviews { get; set; } = new();
    public List<ProductListDto> RelatedFromStore { get; set; } = new();

    /// <summary>True if the current user has the product in their wishlist.</summary>
    public bool IsInWishlist { get; set; }

    /// <summary>Product IDs in current user's wishlist (for related-rail hearts).</summary>
    public HashSet<int> WishlistProductIds { get; set; } = new();

    /// <summary>
    /// The current user's review for this product, if any. When non-null,
    /// the detail page renders a "Sənin rəyin" card with edit/delete actions
    /// and hides the "Rəy yaz" CTA / form.
    /// </summary>
    public ReviewDto? MyReview { get; set; }
}
