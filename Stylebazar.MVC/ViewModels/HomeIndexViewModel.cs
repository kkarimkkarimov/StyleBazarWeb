using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.MVC.ViewModels;

public class HomeIndexViewModel
{
    public List<CategoryTreeDto> Categories { get; set; } = new();
    public List<ProductListDto> FeaturedProducts { get; set; } = new();
    public List<ProductListDto> LatestProducts { get; set; } = new();
}
