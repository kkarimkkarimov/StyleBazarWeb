using System.ComponentModel.DataAnnotations;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.MVC.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad-soyad daxil edin.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad-soyad 2 ilə 100 simvol arası olmalıdır.")]
    [Display(Name = "Ad-soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email daxil edin.")]
    [EmailAddress(ErrorMessage = "Email düzgün formatda deyil.")]
    [StringLength(256)]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    // NOTE: the runtime FluentValidator (RegisterValidator) re-checks every
    // password rule including digit + lowercase — the [RegularExpression] here
    // is the client-side jQuery-validate hook so the user sees the same Az
    // message before round-tripping.
    [Required(ErrorMessage = "Parol daxil edin.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol minimum 6 simvol olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parol")]
    [RegularExpression(@"^(?=.*[a-zəıöüşçğ])(?=.*\d).+$",
        ErrorMessage = "Parol ən az 1 kiçik hərf və 1 rəqəm içerməlidir.")]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parolu təkrarla.")]
    [Compare(nameof(Password), ErrorMessage = "Parol təkrarı uyğun gəlmir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parolu təkrarla")]
    public string ConfirmPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Rol seç.")]
    public UserRole Role { get; set; } = UserRole.Buyer;

    [Display(Name = "İstifadə şərtlərini qəbul edirəm")]
    [Range(typeof(bool), "true", "true", ErrorMessage = "Davam etmək üçün şərtləri qəbul et.")]
    public bool AcceptTerms { get; set; }
}
