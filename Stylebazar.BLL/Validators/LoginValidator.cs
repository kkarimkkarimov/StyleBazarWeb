using FluentValidation;
using Stylebazar.BLL.DTOs.Authentication;

namespace Stylebazar.BLL.Validators;

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email daxil edin.")
            .EmailAddress().WithMessage("Email düzgün formatda deyil.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Parol daxil edin.");
    }
}
