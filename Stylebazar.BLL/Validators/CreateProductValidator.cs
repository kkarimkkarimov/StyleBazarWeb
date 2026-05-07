using FluentValidation;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.BLL.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name is required.")
            .MinimumLength(2)
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .MaximumLength(220)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$").When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug may contain only lowercase letters, digits and dashes.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Description is required.")
            .MinimumLength(10).WithMessage("Description must be at least 10 characters.")
            .MaximumLength(4000);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than 0.");

        RuleFor(x => x.DiscountPrice)
            .GreaterThan(0).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Discount price must be greater than 0.")
            .LessThan(x => x.Price).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Discount price must be less than price.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0);

        RuleFor(x => x.Brand)
            .MaximumLength(100);

        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("StoreId is required.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("CategoryId is required.");

        RuleForEach(x => x.Sizes).ChildRules(size =>
        {
            size.RuleFor(s => s.Size)
                .NotEmpty().WithMessage("Size label is required.")
                .MaximumLength(20);
            size.RuleFor(s => s.StockQuantity)
                .GreaterThanOrEqualTo(0);
        });
    }
}
