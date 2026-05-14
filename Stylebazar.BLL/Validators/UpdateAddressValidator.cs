using FluentValidation;
using Stylebazar.BLL.DTOs.Addresses;

namespace Stylebazar.BLL.Validators;

public class UpdateAddressValidator : AbstractValidator<UpdateAddressDto>
{
    public UpdateAddressValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(150);
        RuleFor(x => x.Phone)
            .NotEmpty()
            .MaximumLength(30)
            .Matches(@"^[+\d\s\-()]{6,30}$").WithMessage("Telefon nömrəsi düzgün formatda deyil.");
        RuleFor(x => x.City).NotEmpty().MaximumLength(80);
        RuleFor(x => x.AddressLine).NotEmpty().MinimumLength(8).MaximumLength(500);
        RuleFor(x => x.Label).MaximumLength(40);
        RuleFor(x => x.PostalCode).MaximumLength(20);
        RuleFor(x => x.Notes).MaximumLength(500);
    }
}
