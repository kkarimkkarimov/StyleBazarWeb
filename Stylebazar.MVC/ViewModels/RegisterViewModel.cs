using System.ComponentModel.DataAnnotations;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.MVC.ViewModels;

public class RegisterViewModel
{
    [Required(ErrorMessage = "Ad-soyad tələb olunur.")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Ad-soyad 2 ilə 100 simvol arası olmalıdır.")]
    [Display(Name = "Ad-soyad")]
    public string FullName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email tələb olunur.")]
    [EmailAddress(ErrorMessage = "Email düzgün formatda deyil.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parol tələb olunur.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol minimum 6 simvol olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parol")]
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
