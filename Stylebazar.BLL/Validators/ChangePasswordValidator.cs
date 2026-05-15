using FluentValidation;
using Stylebazar.BLL.DTOs.Account;

namespace Stylebazar.BLL.Validators;

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Mövcud parolu daxil edin.");

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("Yeni parol daxil edin.")
            .MinimumLength(6).WithMessage("Parol minimum 6 simvol olmalıdır.")
            .NotEqual(x => x.CurrentPassword).WithMessage("Yeni parol mövcud paroldan fərqli olmalıdır.");

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithMessage("Parol təkrarı uyğun gəlmir.");
    }
}
