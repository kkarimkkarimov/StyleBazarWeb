using FluentValidation;
using Stylebazar.BLL.DTOs.Orders;

namespace Stylebazar.BLL.Validators;

public class PlaceOrderValidator : AbstractValidator<PlaceOrderDto>
{
    public PlaceOrderValidator()
    {
        RuleFor(x => x.AddressId)
            .GreaterThan(0).WithMessage("Çatdırılma ünvanı seçilməyib.");

        RuleFor(x => x.PaymentMethod)
            .IsInEnum().WithMessage("Ödəniş üsulu seçilməyib.");

        RuleFor(x => x.CustomerNote).MaximumLength(500);
    }
}
