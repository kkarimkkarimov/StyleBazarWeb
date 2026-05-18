using System.ComponentModel.DataAnnotations;

namespace Stylebazar.MVC.ViewModels;

public class ForgotPasswordViewModel
{
    [Required(ErrorMessage = "Email daxil edin.")]
    [EmailAddress(ErrorMessage = "Email düzgün formatda deyil.")]
    [Display(Name = "Email")]
    public string Email { get; set; } = string.Empty;
}

public class ResetPasswordViewModel
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    public string Token { get; set; } = string.Empty;

    [Required(ErrorMessage = "Yeni parol daxil edin.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Parol minimum 6 simvol olmalıdır.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni parol")]
    [RegularExpression(@"^(?=.*[a-zəıöüşçğ])(?=.*\d).+$",
        ErrorMessage = "Parol ən az 1 kiçik hərf və 1 rəqəm içerməlidir.")]
    public string NewPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Parolu təkrarla.")]
    [Compare(nameof(NewPassword), ErrorMessage = "Parol təkrarı uyğun gəlmir.")]
    [DataType(DataType.Password)]
    [Display(Name = "Yeni parolu təkrarla")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
