using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Stylebazar.BLL.DTOs.Account;
using Stylebazar.BLL.DTOs.Products;
using Stylebazar.BLL.DTOs.Stores;
using Stylebazar.BLL.Exceptions;
using Stylebazar.BLL.Services.Account;
using Stylebazar.BLL.Services.Addresses;
using Stylebazar.BLL.Services.Categories;
using Stylebazar.BLL.Services.LoginEvents;
using Stylebazar.BLL.Services.Products;
using Stylebazar.BLL.Services.Stores;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Entities.Enums;
using Stylebazar.MVC.ViewModels;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.MVC.Controllers;

/// <summary>
/// "Seller Panel" (image 2) — vendor-scoped pages under /Seller/*.
/// Auth-gated to Seller (or Admin override).
/// </summary>
[Authorize(Roles = "Seller,Admin")]
public class SellerController : Controller
{
    private readonly IAccountService _account;
    private readonly IAddressService _addresses;
    private readonly IStoreService _stores;
    private readonly IProductService _products;
    private readonly ICategoryService _categories;
    private readonly ILoginEventService _events;
    private readonly UserManager<AppUser> _userManager;

    public SellerController(
        IAccountService account,
        IAddressService addresses,
        IStoreService stores,
        IProductService products,
        ICategoryService categories,
        ILoginEventService events,
        UserManager<AppUser> userManager)
    {
        _account = account;
        _addresses = addresses;
        _stores = stores;
        _products = products;
        _categories = categories;
        _events = events;
        _userManager = userManager;
    }

    // Dashboard — overview placeholder for Phase 6c (stats + recent activity).
    [HttpGet]
    public async Task<IActionResult> Dashboard(CancellationToken ct)
    {
        return View(await BuildPanelVmAsync(ct));
    }

    // ============== PROFILE (image 2) ==============

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        var vm = await BuildPanelVmAsync(ct);
        var sessions = await _events.GetMyActiveSessionsAsync(ct);
        var history = await _events.GetMyHistoryAsync(30, ct);
        var addresses = await _addresses.GetMineAsync(ct);

        return View(new SellerProfilePageViewModel
        {
            Profile = vm.Profile,
            PrimaryStore = vm.PrimaryStore,
            ActiveSessions = sessions,
            LoginHistory = history,
            Addresses = addresses
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateProfile(
        [FromForm] string fullName,
        [FromForm] string? phoneNumber,
        [FromForm] string? bio,
        CancellationToken ct)
    {
        try
        {
            var dto = new UpdateProfileDto { FullName = fullName, PhoneNumber = phoneNumber, Bio = bio };
            var updated = await _account.UpdateProfileAsync(dto, ct);
            return Json(new { ok = true, profile = updated });
        }
        catch (ValidationException ex) { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStoreContacts(
        [FromForm] int storeId,
        [FromForm] string? whatsAppNumber,
        [FromForm] string? instagramHandle,
        [FromForm] string? telegramHandle,
        [FromForm] string? facebookHandle,
        [FromForm] string? tikTokHandle,
        CancellationToken ct)
    {
        try
        {
            var existing = await _stores.GetByIdAsync(storeId, ct);
            var update = new UpdateStoreDto
            {
                Name = existing.Name,
                Slug = existing.Slug,
                Description = existing.Description,
                ProfileImageUrl = existing.ProfileImageUrl,
                CoverImageUrl = existing.CoverImageUrl,
                ContactPhone = existing.ContactPhone,
                Address = existing.Address,
                WhatsAppNumber = whatsAppNumber,
                InstagramHandle = instagramHandle,
                TelegramHandle = telegramHandle,
                FacebookHandle = facebookHandle,
                TikTokHandle = tikTokHandle
            };
            await _stores.UpdateAsync(storeId, update, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException) { return NotFound(); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    // ============== STORE QUICK-CREATE (Phase 6c prerequisite) ==============

    /// <summary>
    /// Minimal first-store onboarding — accepts just a Name and lets the
    /// seller move on to adding products. The full Store Settings form
    /// (cover, bio, contact channels, etc.) lives in Phase 6e.
    /// </summary>
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateMyStore([FromForm] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length < 2)
        {
            TempData["Error"] = "Mağaza adı ən az 2 simvol olmalıdır.";
            return RedirectToAction(nameof(Products));
        }

        try
        {
            await _stores.CreateAsync(new CreateStoreDto
            {
                Name = name.Trim()
                // Slug auto-generated from name by the StoreManager.
            }, ct);
            TempData["Success"] = "Mağazan yaradıldı — indi məhsul əlavə edə bilərsən.";
        }
        catch (ValidationException vex)
        {
            TempData["Error"] = string.Join(" • ", vex.Errors.SelectMany(kv => kv.Value));
        }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }

        return RedirectToAction(nameof(Products));
    }

    // ============== PRODUCTS (Phase 6c) ==============

    [HttpGet]
    public async Task<IActionResult> Products(bool? isActive, CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        var mine = await _products.GetMineAsync(isActive: null, ct: ct);

        return View(new SellerProductsPageViewModel
        {
            Profile = panel.Profile,
            PrimaryStore = panel.PrimaryStore,
            Products = isActive switch
            {
                true => mine.Where(p => p.IsActive).ToList(),
                false => mine.Where(p => !p.IsActive).ToList(),
                _ => mine
            },
            ActiveCount = mine.Count(p => p.IsActive),
            InactiveCount = mine.Count(p => !p.IsActive),
            IsActive = isActive,
            HasAnyStore = panel.PrimaryStore is not null
        });
    }

    [HttpGet]
    public async Task<IActionResult> AddProduct(CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        if (panel.PrimaryStore is null)
        {
            TempData["Error"] = "Məhsul əlavə etməzdən əvvəl bir mağaza yaratmalısınız.";
            return RedirectToAction(nameof(Products));
        }

        return View(await BuildProductFormVmAsync(panel, product: null, ct));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> AddProduct(ProductFormInput form, CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        if (panel.PrimaryStore is null)
        {
            TempData["Error"] = "Məhsul əlavə etməzdən əvvəl bir mağaza yaratmalısınız.";
            return RedirectToAction(nameof(Products));
        }

        if (!ModelState.IsValid)
            return View(await BuildProductFormVmAsync(panel, product: null, ct, form));

        try
        {
            CreateProductDto dto = MapFormToCreate(form, panel.PrimaryStore.Id);
            var created = await _products.CreateAsync(dto, ct);
            TempData["Success"] = "Məhsul yaradıldı — indi şəkil yükləyə bilərsiniz.";
            return RedirectToAction(nameof(EditProduct), new { id = created.Id });
        }
        catch (ValidationException vex)
        {
            foreach (var (field, msgs) in vex.Errors)
                foreach (var m in msgs)
                    ModelState.AddModelError(field, m);
            return View(await BuildProductFormVmAsync(panel, product: null, ct, form));
        }
        catch (BusinessException bex)
        {
            ModelState.AddModelError(string.Empty, bex.Message);
            return View(await BuildProductFormVmAsync(panel, product: null, ct, form));
        }
    }

    [HttpGet("Seller/EditProduct/{id:int}")]
    public async Task<IActionResult> EditProduct(int id, CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        try
        {
            var product = await _products.GetByIdAsync(id, ct);
            return View(await BuildProductFormVmAsync(panel, product, ct));
        }
        catch (NotFoundException)
        {
            TempData["Error"] = "Məhsul tapılmadı.";
            return RedirectToAction(nameof(Products));
        }
    }

    [HttpPost("Seller/EditProduct/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> EditProduct(int id, ProductFormInput form, CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);

        ProductDetailDto? existing = null;
        try { existing = await _products.GetByIdAsync(id, ct); }
        catch (NotFoundException)
        {
            TempData["Error"] = "Məhsul tapılmadı.";
            return RedirectToAction(nameof(Products));
        }

        if (!ModelState.IsValid)
            return View(await BuildProductFormVmAsync(panel, existing, ct, form));

        try
        {
            UpdateProductDto dto = MapFormToUpdate(form);
            await _products.UpdateAsync(id, dto, ct);
            TempData["Success"] = "Məhsul yeniləndi.";
            return RedirectToAction(nameof(EditProduct), new { id });
        }
        catch (ValidationException vex)
        {
            foreach (var (field, msgs) in vex.Errors)
                foreach (var m in msgs)
                    ModelState.AddModelError(field, m);
            return View(await BuildProductFormVmAsync(panel, existing, ct, form));
        }
        catch (BusinessException bex)
        {
            ModelState.AddModelError(string.Empty, bex.Message);
            return View(await BuildProductFormVmAsync(panel, existing, ct, form));
        }
    }

    [HttpPost("Seller/DeleteProduct/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProduct(int id, CancellationToken ct)
    {
        try
        {
            await _products.DeleteAsync(id, ct);
            TempData["Success"] = "Məhsul silindi.";
        }
        catch (NotFoundException) { TempData["Error"] = "Məhsul tapılmadı."; }
        catch (BusinessException ex) { TempData["Error"] = ex.Message; }
        return RedirectToAction(nameof(Products));
    }

    [HttpPost("Seller/ToggleProductActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleProductActive(int id, CancellationToken ct)
    {
        try
        {
            var updated = await _products.ToggleActiveAsync(id, ct);
            return Json(new { ok = true, isActive = updated.IsActive });
        }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost("Seller/Products/{productId:int}/Images/Upload")]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadProductImage(int productId, IFormFile? file, [FromForm] bool isMain, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { ok = false, error = "Şəkil seçilməyib." });

        try
        {
            var img = await _products.AddImageAsync(productId, file, isMain, ct);
            return Json(new { ok = true, image = img });
        }
        catch (ValidationException ex)  { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost("Seller/Products/{productId:int}/Images/{imageId:int}/Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteProductImage(int productId, int imageId, CancellationToken ct)
    {
        try
        {
            await _products.DeleteImageAsync(productId, imageId, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost("Seller/Products/{productId:int}/Images/{imageId:int}/SetMain")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> SetMainProductImage(int productId, int imageId, CancellationToken ct)
    {
        try
        {
            var img = await _products.SetMainImageAsync(productId, imageId, ct);
            return Json(new { ok = true, image = img });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    // ============== OTHER STUB PAGES ==============

    [HttpGet] public async Task<IActionResult> SmartUpload(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Reviews(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Notifications(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Messages(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Packages(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Integration(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    [HttpGet] public async Task<IActionResult> Analytics(CancellationToken ct)
        => View(await BuildPanelVmAsync(ct));

    // ============== STORE SETTINGS (Phase 6e) ==============

    [HttpGet]
    public async Task<IActionResult> StoreSettings(CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        if (panel.PrimaryStore is null)
        {
            // No store yet → bounce to Products page where the quick-create
            // form lives. Single onboarding entry point.
            TempData["Error"] = "Əvvəlcə bir mağaza yarat.";
            return RedirectToAction(nameof(Products));
        }

        return View(BuildStoreSettingsVm(panel, populateFromStore: true));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> StoreSettings(StoreSettingsFormInput form, CancellationToken ct)
    {
        var panel = await BuildPanelVmAsync(ct);
        if (panel.PrimaryStore is null)
        {
            TempData["Error"] = "Əvvəlcə bir mağaza yarat.";
            return RedirectToAction(nameof(Products));
        }

        if (!ModelState.IsValid)
            return View(BuildStoreSettingsVm(panel, populateFromStore: false, overrideForm: form));

        try
        {
            var dto = new UpdateStoreDto
            {
                Name = form.Name,
                Slug = form.Slug,
                Description = form.Description,
                // Don't touch image URLs from the text form — the dedicated
                // upload endpoints own those fields.
                ProfileImageUrl = null,
                CoverImageUrl = panel.PrimaryStore.CoverImageUrl,
                ContactPhone = form.ContactPhone,
                Address = form.Address,
                WhatsAppNumber = form.WhatsAppNumber,
                InstagramHandle = form.InstagramHandle,
                TelegramHandle = form.TelegramHandle,
                FacebookHandle = form.FacebookHandle,
                TikTokHandle = form.TikTokHandle
            };
            await _stores.UpdateAsync(panel.PrimaryStore.Id, dto, ct);
            TempData["Success"] = "Mağaza məlumatları yeniləndi.";
            return RedirectToAction(nameof(StoreSettings));
        }
        catch (ValidationException vex)
        {
            foreach (var (field, msgs) in vex.Errors)
                foreach (var m in msgs)
                    ModelState.AddModelError(field, m);
            return View(BuildStoreSettingsVm(panel, populateFromStore: false, overrideForm: form));
        }
        catch (BusinessException bex)
        {
            ModelState.AddModelError(string.Empty, bex.Message);
            return View(BuildStoreSettingsVm(panel, populateFromStore: false, overrideForm: form));
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(5 * 1024 * 1024)]
    public async Task<IActionResult> UploadStoreProfileImage([FromForm] int storeId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { ok = false, error = "Şəkil seçilməyib." });

        try
        {
            string url = await _stores.UploadProfileImageAsync(storeId, file, ct);
            return Json(new { ok = true, url });
        }
        catch (ValidationException ex)  { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(10 * 1024 * 1024)]
    public async Task<IActionResult> UploadStoreCoverImage([FromForm] int storeId, IFormFile? file, CancellationToken ct)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { ok = false, error = "Şəkil seçilməyib." });

        try
        {
            string url = await _stores.UploadCoverImageAsync(storeId, file, ct);
            return Json(new { ok = true, url });
        }
        catch (ValidationException ex)  { return BadRequest(new { ok = false, errors = ex.Errors }); }
        catch (NotFoundException ex)    { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)    { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> RemoveStoreCoverImage([FromForm] int storeId, CancellationToken ct)
    {
        try
        {
            await _stores.RemoveCoverImageAsync(storeId, ct);
            return Json(new { ok = true });
        }
        catch (NotFoundException ex) { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex) { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleStoreActive([FromForm] int storeId, CancellationToken ct)
    {
        try
        {
            var updated = await _stores.ToggleActiveAsync(storeId, ct);
            return Json(new { ok = true, isActive = updated.IsActive });
        }
        catch (NotFoundException ex)   { return NotFound(new { ok = false, error = ex.Message }); }
        catch (BusinessException ex)   { return UnprocessableEntity(new { ok = false, error = ex.Message }); }
    }

    // -------- helpers --------

    private async Task<SellerPanelViewModel> BuildPanelVmAsync(CancellationToken ct)
    {
        var profile = await _account.GetMineAsync(ct);
        var stores = await _stores.GetMyStoresAsync(ct);
        StoreDetailDto? primary = null;
        if (stores.Any())
        {
            try { primary = await _stores.GetByIdAsync(stores.First().Id, ct); }
            catch { /* swallow — primary is optional */ }
        }
        return new SellerPanelViewModel { Profile = profile, PrimaryStore = primary };
    }

    private async Task<SellerProductFormViewModel> BuildProductFormVmAsync(
        SellerPanelViewModel panel,
        ProductDetailDto? product,
        CancellationToken ct,
        ProductFormInput? overrideForm = null)
    {
        var categories = await _categories.GetListAsync(
            new Stylebazar.BLL.DTOs.Categories.CategoryListQueryDto { Page = 1, PageSize = 200 },
            ct);

        ProductFormInput form = overrideForm ?? (product is null
            ? new ProductFormInput()
            : new ProductFormInput
            {
                Name = product.Name,
                Slug = product.Slug,
                Description = product.Description,
                Price = product.Price,
                DiscountPrice = product.DiscountPrice,
                StockQuantity = product.StockQuantity,
                Brand = product.Brand,
                Condition = product.Condition,
                CategoryId = product.CategoryId,
                HasDelivery = product.HasDelivery,
                IsActive = product.IsActive,
                Material = product.Material,
                Pattern = product.Pattern,
                Neckline = product.Neckline,
                SleeveLength = product.SleeveLength,
                Fit = product.Fit,
                Season = product.Season,
                Style = product.Style,
                CountryOfOrigin = product.CountryOfOrigin,
                Sizes = product.Sizes
                    .Select(s => new ProductFormSizeRow { Size = s.Size, StockQuantity = s.StockQuantity })
                    .ToList(),
                Colors = product.Colors
                    .OrderBy(c => c.DisplayOrder)
                    .Select(c => new ProductFormColorRow { Name = c.Name, Hex = c.Hex })
                    .ToList()
            });

        return new SellerProductFormViewModel
        {
            Profile = panel.Profile,
            PrimaryStore = panel.PrimaryStore,
            Categories = categories.Items.ToList(),
            IsEdit = product is not null,
            Product = product,
            Form = form
        };
    }

    private SellerStoreSettingsViewModel BuildStoreSettingsVm(
        SellerPanelViewModel panel,
        bool populateFromStore,
        StoreSettingsFormInput? overrideForm = null)
    {
        var st = panel.PrimaryStore!;
        var form = overrideForm ?? (populateFromStore
            ? new StoreSettingsFormInput
            {
                Name = st.Name,
                Slug = st.Slug,
                Description = st.Description,
                ContactPhone = st.ContactPhone,
                Address = st.Address,
                WhatsAppNumber = st.WhatsAppNumber,
                InstagramHandle = st.InstagramHandle,
                TelegramHandle = st.TelegramHandle,
                FacebookHandle = st.FacebookHandle,
                TikTokHandle = st.TikTokHandle
            }
            : new StoreSettingsFormInput());

        return new SellerStoreSettingsViewModel
        {
            Profile = panel.Profile,
            PrimaryStore = panel.PrimaryStore,
            Store = panel.PrimaryStore,
            Form = form
        };
    }

    private static CreateProductDto MapFormToCreate(ProductFormInput f, int storeId) => new()
    {
        Name = f.Name,
        Slug = f.Slug,
        Description = f.Description,
        Price = f.Price,
        DiscountPrice = f.DiscountPrice,
        StockQuantity = f.StockQuantity,
        Brand = f.Brand,
        Condition = f.Condition,
        StoreId = storeId,
        CategoryId = f.CategoryId,
        HasDelivery = f.HasDelivery,
        Material = f.Material,
        Pattern = f.Pattern,
        Neckline = f.Neckline,
        SleeveLength = f.SleeveLength,
        Fit = f.Fit,
        Season = f.Season,
        Style = f.Style,
        CountryOfOrigin = f.CountryOfOrigin,
        Sizes = (f.Sizes ?? new())
            .Where(s => !string.IsNullOrWhiteSpace(s.Size))
            .Select(s => new CreateProductSizeDto { Size = s.Size, StockQuantity = s.StockQuantity })
            .ToList(),
        Colors = (f.Colors ?? new())
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new CreateProductColorDto { Name = c.Name, Hex = c.Hex })
            .ToList()
    };

    private static UpdateProductDto MapFormToUpdate(ProductFormInput f) => new()
    {
        Name = f.Name,
        Slug = f.Slug,
        Description = f.Description,
        Price = f.Price,
        DiscountPrice = f.DiscountPrice,
        StockQuantity = f.StockQuantity,
        Brand = f.Brand,
        Condition = f.Condition,
        CategoryId = f.CategoryId,
        IsActive = f.IsActive,
        HasDelivery = f.HasDelivery,
        Material = f.Material,
        Pattern = f.Pattern,
        Neckline = f.Neckline,
        SleeveLength = f.SleeveLength,
        Fit = f.Fit,
        Season = f.Season,
        Style = f.Style,
        CountryOfOrigin = f.CountryOfOrigin,
        Sizes = (f.Sizes ?? new())
            .Where(s => !string.IsNullOrWhiteSpace(s.Size))
            .Select(s => new CreateProductSizeDto { Size = s.Size, StockQuantity = s.StockQuantity })
            .ToList(),
        Colors = (f.Colors ?? new())
            .Where(c => !string.IsNullOrWhiteSpace(c.Name))
            .Select(c => new CreateProductColorDto { Name = c.Name, Hex = c.Hex })
            .ToList()
    };
}
