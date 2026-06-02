using FluentValidation;
using Stylebazar.BLL.DTOs.Categories;

namespace Stylebazar.BLL.Validators;

// CreateCategoryValidator — FluentValidation qaydaları; müvafiq DTO-nun sahələrinin düzgünlüyünü yoxlayır.
public class CreateCategoryValidator : AbstractValidator<CreateCategoryDto>
{
    public CreateCategoryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Category name is required.")
            .MaximumLength(100).WithMessage("Category name cannot exceed 100 characters.");

        RuleFor(x => x.Slug)
            .MaximumLength(120).WithMessage("Slug cannot exceed 120 characters.")
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$").When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug may contain only lowercase letters, digits and dashes.");

        RuleFor(x => x.ParentCategoryId)
            .GreaterThan(0).When(x => x.ParentCategoryId.HasValue)
            .WithMessage("Parent category id must be positive.");
    }
}
