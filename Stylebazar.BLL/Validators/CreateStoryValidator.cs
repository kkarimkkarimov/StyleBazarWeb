using FluentValidation;
using Stylebazar.BLL.DTOs.Stories;

namespace Stylebazar.BLL.Validators;

public class CreateStoryValidator : AbstractValidator<CreateStoryDto>
{
    /// <summary>Max video length so the viewer doesn't stall on long uploads.</summary>
    public const int MaxVideoSeconds = 30;

    public CreateStoryValidator()
    {
        RuleFor(x => x.Caption).MaximumLength(500);
        RuleFor(x => x.LinkUrl).MaximumLength(500);

        RuleFor(x => x.HoursToExpire)
            .GreaterThan(0).When(x => x.HoursToExpire.HasValue)
            .LessThanOrEqualTo(168).When(x => x.HoursToExpire.HasValue)
            .WithMessage("Bitmə müddəti maksimum 168 saat (7 gün) ola bilər.");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0).When(x => x.DurationSeconds.HasValue)
            .LessThanOrEqualTo(MaxVideoSeconds).When(x => x.DurationSeconds.HasValue)
            .WithMessage($"Video uzunluğu {MaxVideoSeconds} saniyədən çox ola bilməz.");
    }
}
