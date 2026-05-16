using FluentValidation;
using Stylebazar.BLL.DTOs.Products;

namespace Stylebazar.BLL.Validators;

public class CreateProductValidator : AbstractValidator<CreateProductDto>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Məhsulun adı boş ola bilməz.")
            .MinimumLength(2).WithMessage("Məhsulun adı ən az 2 simvol olmalıdır.")
            .MaximumLength(200);

        RuleFor(x => x.Slug)
            .MaximumLength(220)
            .Matches(@"^[a-z0-9]+(-[a-z0-9]+)*$").When(x => !string.IsNullOrWhiteSpace(x.Slug))
            .WithMessage("Slug yalnız kiçik hərf, rəqəm və tire içerə bilər.");

        RuleFor(x => x.Description)
            .NotEmpty().WithMessage("Təsvir boş ola bilməz.")
            .MinimumLength(10).WithMessage("Təsvir ən az 10 simvol olmalıdır.")
            .MaximumLength(4000);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Qiymət 0-dan böyük olmalıdır.");

        RuleFor(x => x.DiscountPrice)
            .GreaterThan(0).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Endirimli qiymət 0-dan böyük olmalıdır.")
            .LessThan(x => x.Price).When(x => x.DiscountPrice.HasValue)
            .WithMessage("Endirimli qiymət əsas qiymətdən kiçik olmalıdır.");

        RuleFor(x => x.StockQuantity)
            .GreaterThanOrEqualTo(0).WithMessage("Stok mənfi ola bilməz.");

        RuleFor(x => x.Brand).MaximumLength(100);

        RuleFor(x => x.StoreId)
            .GreaterThan(0).WithMessage("Mağaza seçilməyib.");

        RuleFor(x => x.CategoryId)
            .GreaterThan(0).WithMessage("Kateqoriya seçilməyib.");

        // 8 features — all optional, length-capped to fit DB columns
        RuleFor(x => x.Material).MaximumLength(80);
        RuleFor(x => x.Pattern).MaximumLength(80);
        RuleFor(x => x.Neckline).MaximumLength(80);
        RuleFor(x => x.SleeveLength).MaximumLength(80);
        RuleFor(x => x.Fit).MaximumLength(80);
        RuleFor(x => x.Season).MaximumLength(80);
        RuleFor(x => x.Style).MaximumLength(80);
        RuleFor(x => x.CountryOfOrigin).MaximumLength(80);

        RuleForEach(x => x.Sizes).ChildRules(size =>
        {
            size.RuleFor(s => s.Size)
                .NotEmpty().WithMessage("Ölçü adı boş ola bilməz.")
                .MaximumLength(20);
            size.RuleFor(s => s.StockQuantity)
                .GreaterThanOrEqualTo(0).WithMessage("Stok mənfi ola bilməz.");
        });

        RuleForEach(x => x.Colors).ChildRules(color =>
        {
            color.RuleFor(c => c.Name)
                .NotEmpty().WithMessage("Rəng adı boş ola bilməz.")
                .MaximumLength(40);
            color.RuleFor(c => c.Hex)
                .NotEmpty()
                .Matches(@"^#[0-9A-Fa-f]{6}$").WithMessage("Rəng kodu #RRGGBB formatında olmalıdır.");
        });
    }
}
