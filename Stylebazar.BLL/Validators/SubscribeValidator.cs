using FluentValidation;
using Stylebazar.BLL.DTOs.Newsletter;

namespace Stylebazar.BLL.Validators;

// SubscribeValidator — FluentValidation qaydaları; müvafiq DTO-nun sahələrinin düzgünlüyünü yoxlayır.
public class SubscribeValidator : AbstractValidator<SubscribeDto>
{
    public SubscribeValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email tələb olunur.")
            .EmailAddress().WithMessage("Email düzgün formatda deyil.")
            .MaximumLength(256);
    }
}
