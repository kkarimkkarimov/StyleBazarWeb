using AutoMapper;
using Core.Persistence.Paging;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.EntityFrameworkCore;
using Stylebazar.BLL.DTOs.Categories;
using Stylebazar.BLL.Helpers;
using Stylebazar.BLL.Rules;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;
using ValidationException = Stylebazar.BLL.Exceptions.ValidationException;

namespace Stylebazar.BLL.Services.Categories;

// KATEQORIYA MANAGER — ICategoryService-in GERÇƏKLƏŞMƏSİ (biznes məntiqi qatı).
// Tipik axın: DTO yoxlanışı (validator) → biznes qaydaları (rules) → repository (baza) → DTO-ya map.
// Controller birbaşa bazaya yox, bu xidmətə müraciət edir (qatların ayrılması).
public class CategoryManager : ICategoryService
{
    // Asılılıqlar DI ilə gəlir: repo (baza), mapper (entity↔DTO), rules (qaydalar), validator-lar (yoxlama).
    private readonly ICategoryRepository _repo;
    private readonly IMapper _mapper;
    private readonly CategoryBusinessRules _rules;
    private readonly IValidator<CreateCategoryDto> _createValidator;
    private readonly IValidator<UpdateCategoryDto> _updateValidator;

    public CategoryManager(
        ICategoryRepository repo,
        IMapper mapper,
        CategoryBusinessRules rules,
        IValidator<CreateCategoryDto> createValidator,
        IValidator<UpdateCategoryDto> updateValidator)
    {
        _repo = repo;
        _mapper = mapper;
        _rules = rules;
        _createValidator = createValidator;
        _updateValidator = updateValidator;
    }

    // SİYAHI gətirir — filtr (valideyn/axtarış) + sıralama + səhifələmə ilə, sonra DTO-ya çevirir.
    public async Task<Paginate<CategoryListDto>> GetListAsync(CategoryListQueryDto query, CancellationToken ct = default)
    {
        Paginate<Category> page = await _repo.GetListAsync(
            predicate: c =>
                (!query.ParentCategoryId.HasValue || c.ParentCategoryId == query.ParentCategoryId.Value) &&
                (string.IsNullOrEmpty(query.Search) || c.Name.Contains(query.Search)),
            orderBy: q => q.OrderBy(c => c.Name),
            include: q => q.Include(c => c.ParentCategory!),   // valideyn adını da göstərmək üçün qoş
            index: query.Page - 1,                             // səhifə 1-dən, index 0-dan başlayır
            size: query.PageSize,
            enableTracking: false,                             // yalnız oxuyuruq → sürətli
            cancellationToken: ct);

        // Entity səhifəsini DTO səhifəsinə çevir (kateqoriyaları UI üçün uyğun formata sal).
        return new Paginate<CategoryListDto>
        {
            Index = page.Index,
            Size = page.Size,
            Count = page.Count,
            Pages = page.Pages,
            Items = _mapper.Map<List<CategoryListDto>>(page.Items)
        };
    }

    // Id-yə görə bir kateqoriya (valideyn + uşaqları ilə birlikdə) gətirir.
    public async Task<CategoryDetailDto> GetByIdAsync(int id, CancellationToken ct = default)
    {
        Category category = await GetWithChildrenAsync(c => c.Id == id, ct);
        return _mapper.Map<CategoryDetailDto>(category);
    }

    // Slug-a (URL adına) görə kateqoriya gətirir — açıq səhifələrdə URL ilə açılır.
    public async Task<CategoryDetailDto> GetBySlugAsync(string slug, CancellationToken ct = default)
    {
        Category category = await GetWithChildrenAsync(c => c.Slug == slug, ct);
        return _mapper.Map<CategoryDetailDto>(category);
    }

    // Bütün kateqoriyaları AĞAC (tree) quruluşunda qurur: kök kateqoriyalar + altındakı uşaqlar.
    public async Task<List<CategoryTreeDto>> GetTreeAsync(CancellationToken ct = default)
    {
        // Hamısını bir dəfə gətir (int.MaxValue = limitsiz).
        Paginate<Category> all = await _repo.GetListAsync(
            orderBy: q => q.OrderBy(c => c.Name),
            index: 0,
            size: int.MaxValue,
            enableTracking: false,
            cancellationToken: ct);

        // Id → node lüğəti qur ki, valideyni tez tapaq.
        Dictionary<int, CategoryTreeDto> map = all.Items
            .ToDictionary(c => c.Id, c => new CategoryTreeDto
            {
                Id = c.Id,
                Name = c.Name,
                Slug = c.Slug
            });

        // Hər kateqoriyanı ya valideyninin uşaqlarına, ya da köklərə əlavə et.
        List<CategoryTreeDto> roots = new();
        foreach (Category c in all.Items)
        {
            CategoryTreeDto node = map[c.Id];
            if (c.ParentCategoryId.HasValue && map.TryGetValue(c.ParentCategoryId.Value, out CategoryTreeDto? parent))
                parent.Children.Add(node);   // valideyni var → onun altına
            else
                roots.Add(node);             // valideyni yox → kök kateqoriya
        }

        return roots;
    }

    // YENİ kateqoriya yaradır.
    public async Task<CategoryDetailDto> CreateAsync(CreateCategoryDto dto, CancellationToken ct = default)
    {
        // 1) Giriş məlumatını yoxla (FluentValidation). Səhvdirsə — ValidationException.
        ValidationResult validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 2) Slug verilməyibsə, addan avtomatik düzəlt.
        string slug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();

        // 3) Biznes qaydaları: valideyn varsa mövcud olmalı + slug unikal olmalı.
        await _rules.ParentMustExistIfSpecifiedAsync(dto.ParentCategoryId, ct);
        await _rules.SlugMustBeUniqueAsync(slug, excludeId: null, ct: ct);

        // 4) Entity qur və bazaya əlavə et.
        Category entity = new()
        {
            Name = dto.Name.Trim(),
            Slug = slug,
            ParentCategoryId = dto.ParentCategoryId
        };

        Category created = await _repo.AddAsync(entity, ct);
        return await GetByIdAsync(created.Id, ct);   // yaranmış qeydi DTO kimi geri qaytar
    }

    // Mövcud kateqoriyanı YENİLƏYİR.
    public async Task<CategoryDetailDto> UpdateAsync(int id, UpdateCategoryDto dto, CancellationToken ct = default)
    {
        // 1) Giriş yoxlaması.
        ValidationResult validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
            throw new ValidationException(validation.Errors);

        // 2) Kateqoriya mövcud olmalıdır.
        Category existing = await _rules.CategoryMustExistAsync(id, ct);

        string newSlug = string.IsNullOrWhiteSpace(dto.Slug) ? SlugHelper.Slugify(dto.Name) : dto.Slug.Trim();

        // 3) Bütün biznes qaydaları: öz valideyni olmasın, valideyn mövcud olsun,
        //    dövri istinad yaranmasın, yeni slug unikal olsun.
        _rules.CannotBeOwnParent(id, dto.ParentCategoryId);
        await _rules.ParentMustExistIfSpecifiedAsync(dto.ParentCategoryId, ct);
        await _rules.NoCircularReferenceAsync(id, dto.ParentCategoryId, ct);
        await _rules.SlugMustBeUniqueAsync(newSlug, excludeId: id, ct: ct);

        // 4) Düzgün UPDATE üçün EF tərəfindən İZLƏNƏN (tracked) obyekt lazımdır — yenidən yüklə.
        Category? tracked = await _repo.GetAsync(c => c.Id == id, cancellationToken: ct);
        if (tracked is null)
            throw new Exceptions.NotFoundException(nameof(Category), id);

        // 5) Sahələri dəyiş və yadda saxla.
        tracked.Name = dto.Name.Trim();
        tracked.Slug = newSlug;
        tracked.ParentCategoryId = dto.ParentCategoryId;

        await _repo.UpdateAsync(tracked, ct);
        return await GetByIdAsync(id, ct);
    }

    // Kateqoriyanı SİLİR (yumşaq silmə).
    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        // Mövcud olmalı + uşağı olmamalı + məhsulu olmamalı (yoxsa silmək olmaz).
        Category existing = await _rules.CategoryMustExistAsync(id, ct);

        await _rules.CannotDeleteWhenHasChildrenAsync(id, ct);
        await _rules.CannotDeleteWhenHasProductsAsync(id, ct);

        Category? tracked = await _repo.GetAsync(c => c.Id == id, cancellationToken: ct);
        if (tracked is null)
            return;

        // permanent: false → yumşaq silmə (DeletedDate doldurulur, sətir bazada qalır).
        await _repo.DeleteAsync(tracked, permanent: false, cancellationToken: ct);
    }

    // Köməkçi: kateqoriyanı valideyni + uşaqları ilə birlikdə gətirir (kod təkrarını azaldır).
    private async Task<Category> GetWithChildrenAsync(System.Linq.Expressions.Expression<Func<Category, bool>> predicate, CancellationToken ct)
    {
        Category? category = await _repo.GetAsync(
            predicate,
            include: q => q
                .Include(c => c.ParentCategory!)
                .Include(c => c.ChildCategories),
            enableTracking: false,
            cancellationToken: ct);

        if (category is null)
            throw new Exceptions.NotFoundException(nameof(Category), 0);

        return category;
    }
}
