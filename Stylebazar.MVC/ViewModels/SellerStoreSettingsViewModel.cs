using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.MVC.ViewModels;

/// <summary>
/// /Seller/StoreSettings page. Reuses the standard SellerPanel sidebar +
/// header; carries the full store DTO plus the form-bound POCO.
/// </summary>
public class SellerStoreSettingsViewModel : SellerPanelViewModel
{
    public StoreDetailDto? Store { get; set; }
    public StoreSettingsFormInput Form { get; set; } = new();
}

/// <summary>
/// Form-bound model — kept narrow so DataAnnotations can drive
/// client-side jQuery Unobtrusive validation with the same Az messages
/// the FluentValidator emits server-side.
/// </summary>
public class StoreSettingsFormInput
{
    [System.ComponentModel.DataAnnotations.Required(ErrorMessage = "Mağaza adı boş ola bilməz.")]
    [System.ComponentModel.DataAnnotations.StringLength(100, MinimumLength = 2,
        ErrorMessage = "Mağaza adı 2 ilə 100 simvol arası olmalıdır.")]
    public string Name { get; set; } = string.Empty;

    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[a-z0-9]+(-[a-z0-9]+)*$|^$",
        ErrorMessage = "Slug yalnız kiçik hərf, rəqəm və tire içerə bilər.")]
    [System.ComponentModel.DataAnnotations.StringLength(120)]
    public string? Slug { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(1000,
        ErrorMessage = "Təsvir 1000 simvoldan çox ola bilməz.")]
    public string? Description { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(20)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[\+0-9\s\-\(\)]+$|^$",
        ErrorMessage = "Telefon nömrəsi düzgün formatda deyil.")]
    public string? ContactPhone { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(500,
        ErrorMessage = "Ünvan 500 simvoldan çox ola bilməz.")]
    public string? Address { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(20)]
    [System.ComponentModel.DataAnnotations.RegularExpression(@"^[0-9]+$|^$",
        ErrorMessage = "WhatsApp nömrəsi yalnız rəqəm olmalıdır (məs: 994501234567).")]
    public string? WhatsAppNumber { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string? InstagramHandle { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string? TelegramHandle { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string? FacebookHandle { get; set; }

    [System.ComponentModel.DataAnnotations.StringLength(60)]
    public string? TikTokHandle { get; set; }
}
