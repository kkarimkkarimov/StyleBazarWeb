using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Reviews;
using Stylebazar.BLL.Services.Wishlist;
using Stylebazar.MVC.ViewModels;

namespace Stylebazar.MVC.Controllers;

public class ShopController : Controller
{
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly IReviewService _reviews;
    private readonly IWishlistService _wishlist;

    public ShopController(
        IProductService products,
        ICategoryService categories,
        IReviewService reviews,
        IWishlistService wishlist)
    {
        _products = products;
        _categories = categories;
        _reviews = reviews;
        _wishlist = wishlist;
    }

    public async Task<IActionResult> Index([FromQuery] ProductListQueryDto query, CancellationToken ct)
    {
        if (!User.IsInRole("Admin") && !User.IsInRole("Seller") && !query.IsActive.HasValue)
            query.IsActive = true;

        var products = await _products.GetListAsync(query, ct);
        var catPage = await _categories.GetListAsync(new CategoryListQueryDto { Page = 1, PageSize = 100 }, ct);
        HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

        return View(new ShopIndexViewModel
        {
            Query = query,
            Products = products,
            Categories = catPage.Items.ToList(),
            Brands = products.Items
                .Where(p => !string.IsNullOrWhiteSpace(p.Brand))
                .Select(p => p.Brand!)
                .Distinct()
                .OrderBy(b => b)
                .ToList(),
            WishlistProductIds = wishlistIds
        });
    }

    public async Task<IActionResult> Detail(int id, CancellationToken ct)
    {
        try
        {
            var product = await _products.GetByIdAsync(id, ct);
            var reviews = await _reviews.GetByProductAsync(id,
                new ReviewListQueryDto { Page = 1, PageSize = 10 }, ct);

            // "Daha çox mağazadan" — same store products, excluding current.
            var related = await _products.GetByStoreAsync(product.StoreId, ct);
            related = related.Where(p => p.Id != id).Take(4).ToList();

            // Wishlist state for hearts on this page + on related rail.
            HashSet<int> wishlistIds = await _wishlist.GetMyProductIdsAsync(ct);

            // Current user's review for this product (null if anon or hasn't reviewed).
            ReviewDto? myReview = await _reviews.GetMineForProductAsync(id, ct);

            // If MyReview exists, filter it out of the public list so it
            // doesn't render twice (we show it in its own highlighted card).
            if (myReview is not null)
            {
                reviews.Items = reviews.Items.Where(r => r.Id != myReview.Id).ToList();
            }

            return View(new ShopDetailViewModel
            {
                Product = product,
                Reviews = reviews,
                RelatedFromStore = related,
                IsInWishlist = wishlistIds.Contains(id),
                WishlistProductIds = wishlistIds,
                MyReview = myReview
            });
        }
        catch (NotFoundException)
        {
            return NotFound();
        }
    }
}
