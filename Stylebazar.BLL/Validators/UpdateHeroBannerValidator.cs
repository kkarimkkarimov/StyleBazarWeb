using FluentValidation;
using Stylebazar.BLL.DTOs.HeroBanners;

namespace Stylebazar.BLL.Validators;

// UpdateHeroBannerValidator — FluentValidation qaydaları; müvafiq DTO-nun sahələrinin düzgünlüyünü yoxlayır.
public class UpdateHeroBannerValidator : AbstractValidator<UpdateHeroBannerDto>
{
    public UpdateHeroBannerValidator()
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
