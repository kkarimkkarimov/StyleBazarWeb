using FluentValidation;
using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.BLL.Validators;

public class UpdateStoreValidator : AbstractValidator<UpdateStoreDto>
{
    public UpdateStoreValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Store name is required.")
            .MinimumLength(2).WithMessage("Store name must be at least 2 characters.")
            .MaximumLength(100).WithMessage("Store name cannot exceed 100 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(120)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$").When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug may contain only lowercase letters, digits and dashes.");

        RuleFor(x => x.Description).MaximumLength(1000);
        RuleFor(x => x.ProfileImageUrl).MaximumLength(500);
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20)
            .Matches(@"^[\+0-9\s\-\(\)]+$").When(x => !string.IsNullOrWhiteSpace(x.ContactPhone))
            .WithMessage("Phone number contains invalid characters.");

        RuleFor(x => x.Address).MaximumLength(500);
    }
}
