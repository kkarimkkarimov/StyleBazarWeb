using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// /Seller/Products list page. Reuses SellerPanelViewModel for the shared
/// sidebar + header, adds the grid data and the active filter for the tab bar.
/// </summary>
public class SellerProductsPageViewModel : SellerPanelViewModel
{
    public List<ProductListDto> Products { get; set; } = new();

    /// <summary>Active tab — null = all, true = aktiv, false = deaktiv.</summary>
    public bool? IsActive { get; set; }

    public int ActiveCount { get; set; }
    public int InactiveCount { get; set; }
    public int TotalCount => ActiveCount + InactiveCount;

    /// <summary>True when the seller has at least one store — gates the "Yeni məhsul" button.</summary>
    public bool HasAnyStore { get; set; }
}

/// <summary>
/// Shared between AddProduct (step 1) and EditProduct (step 2). When
/// <see cref="IsEdit"/> is true, <see cref="Product"/> + image gallery
/// + sizes/colors panels render — otherwise only the core fields.
/// </summary>
public class SellerProductFormViewModel : SellerPanelViewModel
{
    public bool IsEdit { get; set; }

    /// <summary>Existing product when editing — drives image gallery + variant rows.</summary>
    public ProductDetailDto? Product { get; set; }

    /// <summary>Flat list of all categories for the dropdown.</summary>
    public List<CategoryListDto> Categories { get; set; } = new();

    /// <summary>Form bindings. Populated from <see cref="Product"/> on GET edit.</summary>
    public ProductFormInput Form { get; set; } = new();
}

/// <summary>
/// Plain POCO bound from the &lt;form&gt; — keeps the controller signature
/// tidy and lets jQuery Unobtrusive validation read DataAnnotations.
/// </summary>
public class ProductFormInput
{
    // ---- Core ----
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Məhsulun adı boş ola bilməz.")]
    [System.ComponentModel.DataAnnotations.StringLength(200, MinimumLength = 2,
        ErrorMessage = "Ad 2 ilə 200 simvol arası olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$|^$",
        ErrorMessage = "Slug yalnız kiçik hərf, rəqəm və tire içerə bilər.")]
    public string? Slug { get; set; }

    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Təsvir boş ola bilməz.")]
    [System.ComponentModel.DataAnnotations.StringLength(4000, MinimumLength = 10,
        ErrorMessage = "Təsvir ən az 10 simvol olmalıdır.")]
    public string Description { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.Range(0.01, 1000000, ErrorMessage = "Qiymət 0-dan böyük olmalıdır.")]
    public decimal Price { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0.01, 1000000, ErrorMessage = "Endirimli qiymət 0-dan böyük olmalıdır.")]
    public decimal? DiscountPrice { get; set; }

    [System.ComponentModel.DataAnnotations.Range(0, 1000000, ErrorMessage = "Stok mənfi ola bilməz.")]
    public int StockQuantity { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(100)]
    public string? Brand { get; set; }

    public Stylebazar.DAL.Entities.Enums.ProductCondition Condition { get; set; }
        = Stylebazar.DAL.Entities.Enums.ProductCondition.New;

    [System.ComponentModel.DataAnnotations.Range(1, int.MaxValue, ErrorMessage = "Kateqoriya seçin.")]
    public int CategoryId { get; set; }

    public bool HasDelivery { get; set; }
    public bool IsActive { get; set; } = true;

    // ---- Variant input — comma-/tag-driven on the client, indexed on POST ----
    public List<ProductFormSizeRow> Sizes { get; set; } = new();
    public List<ProductFormColorRow> Colors { get; set; } = new();

    // ---- 8 free-text feature fields ----
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Material { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Pattern { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Neckline { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? SleeveLength { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Fit { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Season { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? Style { get; set; }
    [System.ComponentModel.DataAnnotations.StringLength(80)] public string? CountryOfOrigin { get; set; }
}

// ProductFormSizeRow — view (səhifə) üçün məlumat modeli; controller doldurur, Razor göstərir.
public class ProductFormSizeRow
{
    public string Size { get; set; } = string.Empty;
    public int StockQuantity { get; set; }
}

// ProductFormColorRow — view (səhifə) üçün məlumat modeli; controller doldurur, Razor göstərir.
public class ProductFormColorRow
{
    public string Name { get; set; } = string.Empty;
    public string Hex { get; set; } = "#000000";
}
