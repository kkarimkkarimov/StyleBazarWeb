using FluentValidation;
using Stylebazar.BLL.DTOs.Analytics;

namespace Stylebazar.BLL.Validators;

public class RecordContactClickValidator : AbstractValidator<RecordContactClickDto>
{
    public RecordContactClickValidator()
    {
        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("StoreId tələb olunur.");

        RuleFor(x => x.ContactType)
            .IsInEnum().WithMessage("Kanal dəyəri etibarsızdır.");

        // ProductId is optional (null for store-header clicks). When present,
        // must be a real positive id — zero is meaningless.
        RuleFor(x => x.ProductId)
            .GreaterThan(0)
            .When(x => x.ProductId.HasValue)
            .WithMessage("ProductId etibarsızdır.");
    }
}
