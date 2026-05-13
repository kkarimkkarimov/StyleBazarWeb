using Stylebazar.BLL.DTOs.Wishlist;

namespace Stylebazar.MVC.ViewModels;

public class WishlistIndexViewModel
{
    public List<WishlistItemDto> Items { get; set; } = new();

    /// <summary>Convenience for empty-state branch.</summary>
    public bool HasItems => Items.Count > 0;
}
