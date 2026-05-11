using FluentValidation;
using Stylebazar.BLL.DTOs.HeroBanners;

namespace Stylebazar.BLL.Validators;

public class CreateHeroBannerValidator : AbstractValidator<CreateHeroBannerDto>
{
    public CreateHeroBannerValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Subtitle).MaximumLength(500);
        RuleFor(x => x.Eyebrow).MaximumLength(100);
        RuleFor(x => x.ImageUrl).MaximumLength(500);
        RuleFor(x => x.CtaText).MaximumLength(100);
        RuleFor(x => x.CtaUrl).MaximumLength(500);
        RuleFor(x => x.DisplayOrder).GreaterThanOrEqualTo(0);
    }
}
