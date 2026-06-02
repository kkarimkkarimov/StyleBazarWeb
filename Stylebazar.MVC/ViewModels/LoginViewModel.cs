using System.ComponentModel.DataAnnotations;

namespace Stylebazar.MVC.ViewModels;

// LoginViewModel — view (səhifə) üçün məlumat modeli. Controller doldurur, Razor səhifəsi göstərir.
public class LoginViewModel
{
    [Required(ErrorMessage = "Email tələb olunur.")]
    [EmailAddress(ErrorMessage = "Email düzgün formatda deyil.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parol tələb olunur.")]
    [DataType(DataType.Password)]
    [Display(Name = "Parol")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Məni xatırla")]
    public bool RememberMe { get; set; }
}
