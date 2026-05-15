using FluentValidation;
using Stylebazar.BLL.DTOs.Account;

namespace Stylebazar.BLL.Validators;

public class UpdateProfileValidator : AbstractValidator<UpdateProfileDto>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad-soyad daxil edin.")
            .MaximumLength(100);

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(30)
            .Matches(@"^[+\d\s\-()]{6,30}$").When(x => !string.IsNullOrWhiteSpace(x.PhoneNumber))
            .WithMessage("Telefon nömrəsi düzgün formatda deyil.");

        RuleFor(x => x.Bio).MaximumLength(500);
    }
}
