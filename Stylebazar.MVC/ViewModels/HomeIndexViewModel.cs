using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.HeroBanners;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.MVC.ViewModels;

public class HomeIndexViewModel
{
    public List<HeroBannerDto> Banners { get; set; } = new();
    public List<StoreStoriesDto> Stories { get; set; } = new();
    public List<CategoryTreeDto> Categories { get; set; } = new();
    public List<ProductListDto> FeaturedProducts { get; set; } = new();
    public List<ProductListDto> LatestProducts { get; set; } = new();
}
