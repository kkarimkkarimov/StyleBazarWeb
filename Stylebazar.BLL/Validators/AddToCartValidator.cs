using FluentValidation;
using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.BLL.Validators;

public class AddToCartValidator : AbstractValidator<AddToCartDto>
{
    public AddToCartValidator()
    {
        RuleFor(x => x.ProductId)
            .GreaterThan(0).WithMessage("Məhsul seçilməyib.");

        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miqdar minimum 1 olmalıdır.")
            .LessThanOrEqualTo(99).WithMessage("Bir əməliyyatda maksimum 99 ədəd əlavə oluna bilər.");

        RuleFor(x => x.ProductSizeId)
            .GreaterThan(0).When(x => x.ProductSizeId.HasValue)
            .WithMessage("Seçilən ölçü etibarsızdır.");
    }
}
