using FluentValidation;
using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.BLL.Validators;

public class CreateAddressValidator : AbstractValidator<CreateAddressDto>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Ad-soyad daxil edin.")
            .MaximumLength(150);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Telefon nömrəsi daxil edin.")
            .MaximumLength(30)
            .Matches(@"^[+\d\s\-()]{6,30}$").WithMessage("Telefon nömrəsi düzgün formatda deyil.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şəhər daxil edin.")
            .MaximumLength(80);

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Ünvan tam yazılmalıdır.")
            .MinimumLength(8).WithMessage("Ünvan minimum 8 simvol olmalıdır.")
            .MaximumLength(500);

        RuleFor(x => x.Label).MaximumLength(40);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
