using FluentValidation;
using Stylebazar.BLL.DTOs.Subscriptions;
using Stylebazar.DAL.Entities.Enums;

namespace Stylebazar.BLL.Validators;

public class UpgradeSubscriptionValidator : AbstractValidator<UpgradeSubscriptionDto>
{
    public UpgradeSubscriptionValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("İstifadəçi seçilməyib.");

        RuleFor(x => x.Plan)
            .IsInEnum().WithMessage("Plan dəyəri etibarsızdır.");

        // ExpiresAt: only required for paid plans, and must be in the future.
        // Free downgrades clear the field — validator allows null there.
        RuleFor(x => x.ExpiresAt)
            .Must((dto, expiresAt) =>
                dto.Plan == SubscriptionPlan.Free || expiresAt == null || expiresAt > DateTime.UtcNow)
            .WithMessage("Bitmə tarixi gələcəkdə olmalıdır.");

        RuleFor(x => x.Notes)
            .MaximumLength(1000).WithMessage("Qeyd 1000 simvoldan çox ola bilməz.");
    }
}
