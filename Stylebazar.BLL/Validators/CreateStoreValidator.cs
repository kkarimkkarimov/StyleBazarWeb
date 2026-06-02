using FluentValidation;
using Stylebazar.BLL.DTOs.Stores;

namespace Stylebazar.BLL.Validators;

// CreateStoreValidator — FluentValidation qaydaları; müvafiq DTO-nun sahələrinin düzgünlüyünü yoxlayır.
public class CreateStoreValidator : AbstractValidator<CreateStoreDto>
{
    public CreateStoreValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Mağaza adı boş ola bilməz.")
            .MinimumLength(2).WithMessage("Mağaza adı ən az 2 simvol olmalıdır.")
            .MaximumLength(100).WithMessage("Mağaza adı 100 simvoldan çox ola bilməz.");

        RuleFor(x => x.Slug)
            .MaximumLength(120)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$").When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug yalnız kiçik hərf, rəqəm və tire içerə bilər.");

        RuleFor(x => x.Description).MaximumLength(1000)
            .WithMessage("Təsvir 1000 simvoldan çox ola bilməz.");

        RuleFor(x => x.ProfileImageUrl).MaximumLength(500);
        RuleFor(x => x.CoverImageUrl).MaximumLength(500);

        RuleFor(x => x.ContactPhone)
            .MaximumLength(20)
            .Matches(@"^[\+0-9\s\-\(\)]+$").When(x => !string.IsNullOrWhiteSpace(x.ContactPhone))
            .WithMessage("Telefon nömrəsi düzgün formatda deyil.");

        RuleFor(x => x.Address).MaximumLength(500)
            .WithMessage("Ünvan 500 simvoldan çox ola bilməz.");
    }
}
