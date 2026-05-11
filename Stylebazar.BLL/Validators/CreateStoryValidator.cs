using FluentValidation;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.BLL.Validators;

public class CreateStoryValidator : AbstractValidator<CreateStoryDto>
{
    public CreateStoryValidator()
    {
        RuleFor(x => x.Caption).MaximumLength(500);
        RuleFor(x => x.LinkUrl).MaximumLength(500);
        RuleFor(x => x.HoursToExpire)
            .GreaterThan(0).When(x => x.HoursToExpire.HasValue)
            .LessThanOrEqualTo(168).When(x => x.HoursToExpire.HasValue)
            .WithMessage("Expiry can be at most 168 hours (7 days).");
    }
}
