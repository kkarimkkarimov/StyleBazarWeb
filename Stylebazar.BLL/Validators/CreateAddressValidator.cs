using FluentValidation;
using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.BLL.Validators;

public class CreateAddressValidator : AbstractValidator<CreateAddressDto>
{
    public CreateAddressValidator()
    {
        RuleFor(x => x.Title)
            .NotEmpty().WithMessage("Ünvan adı daxil edin (məs: Ev, Ofis).")
            .MaximumLength(60);

        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Qəbul edənin ad-soyadı boş ola bilməz.")
            .MaximumLength(100);

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("Əlaqə nömrəsi boş ola bilməz.")
            .MaximumLength(30)
            .Matches(@"^[+\d\s\-()]{6,30}$")
            .WithMessage("Telefon nömrəsi düzgün formatda deyil.");

        RuleFor(x => x.City)
            .NotEmpty().WithMessage("Şəhər daxil edin.")
            .MaximumLength(80);

        RuleFor(x => x.District).MaximumLength(80);

        RuleFor(x => x.AddressLine)
            .NotEmpty().WithMessage("Ünvan sətri boş ola bilməz.")
            .MaximumLength(300);

        RuleFor(x => x.PostalCode).MaximumLength(16);
        RuleFor(x => x.Notes).MaximumLength(300);
    }
}
