using FluentValidation;
using Stylebazar.BLL.DTOs.Cart;

namespace Stylebazar.BLL.Validators;

public class UpdateCartItemValidator : AbstractValidator<UpdateCartItemDto>
{
    public UpdateCartItemValidator()
    {
        RuleFor(x => x.Quantity)
            .GreaterThan(0).WithMessage("Miqdar minimum 1 olmalıdır.")
            .LessThanOrEqualTo(99).WithMessage("Maksimum 99 ədəd seçilə bilər.");
    }
}
