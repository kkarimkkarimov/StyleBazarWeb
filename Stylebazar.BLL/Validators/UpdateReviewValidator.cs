using FluentValidation;
using Stylebazar.BLL.DTOs.Reviews;

namespace Stylebazar.BLL.Validators;

public class UpdateReviewValidator : AbstractValidator<UpdateReviewDto>
{
    public UpdateReviewValidator()
    {
        RuleFor(x => x.Rating)
            .InclusiveBetween(1, 5).WithMessage("Rating must be between 1 and 5.");

        RuleFor(x => x.Comment)
            .MaximumLength(2000);
    }
}
