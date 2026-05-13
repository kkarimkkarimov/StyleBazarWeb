using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Reviews;

namespace Stylebazar.MVC.ViewModels;

public class ShopDetailViewModel
{
    public ProductDetailDto Product { get; set; } = new();
    public Paginate<ReviewDto> Reviews { get; set; } = new();
    public List<ProductListDto> RelatedFromStore { get; set; } = new();
}
