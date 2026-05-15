using FluentValidation;
using Stylebazar.BLL.DTOs.Authentication;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Validators;

/// <summary>
/// Single source of truth for register-shape validation. Messages are Azerbaijani
/// so they can be surfaced as-is in the UI; password complexity rules mirror
/// the Identity <c>PasswordOptions</c> in <c>AddIdentityServices()</c> so the
/// user sees the failure here (next to the input) instead of one round-trip
/// later from Identity.
/// </summary>
public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad-soyad daxil edin.")
            .MaximumLength(100).WithMessage("Ad-soyad 100 simvoldan çox ola bilməz.")
            .Must(s => s.Trim().Length >= 2).WithMessage("Ad-soyad ən az 2 simvol olmalıdır.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email daxil edin.")
            .EmailAddress().WithMessage("Email düzgün formatda deyil.")
            .MaximumLength(256);

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parol daxil edin.")
            .MinimumLength(6).WithMessage("Parol minimum 6 simvol olmalıdır.")
            .Matches(@"[a-zəıöüşçğ]")
                .WithMessage("Parol ən az 1 kiçik hərf içerməlidir.")
            .Matches(@"\d")
                .WithMessage("Parol ən az 1 rəqəm içerməlidir.")
            .MaximumLength(100);

        RuleFor(x => x.ConfirmPassword)
            .NotEmpty().WithMessage("Parolu təkrarla.")
            .Equal(x => x.Password).WithMessage("Parol təkrarı uyğun gəlmir.");

        RuleFor(x => x.Role)
            .Must(r => r == UserRole.Buyer || r == UserRole.Seller)
            .WithMessage("Rol Alıcı və ya Satıcı olmalıdır.");
    }
}
