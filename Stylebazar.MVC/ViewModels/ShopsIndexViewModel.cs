using Core.Persistence.Paging;
using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.MVC.ViewModels;

// ShopsIndexViewModel — view (səhifə) üçün məlumat modeli. Controller doldurur, Razor səhifəsi göstərir.
public class ShopsIndexViewModel
{
    public Paginate<StoreListDto> Stores { get; set; } = new();
    public string? Search { get; set; }
    public int Page { get; set; } = 1;
}
