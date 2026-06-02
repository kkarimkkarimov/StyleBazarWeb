using Stylebazar.BLL.Constants;
using Stylebazar.BLL.Exceptions;
using Stylebazar.DAL.Entities;
using Stylebazar.DAL.Repositories.Abstract;

namespace Stylebazar.BLL.Rules;

// KATEQORIYA BİZNES QAYDALARI — kateqoriya əməliyyatlarından əvvəl yoxlanılan şərtlər.
// Hər metod BİR qaydanı yoxlayır; qayda pozularsa xəta (BusinessException / NotFound) atır.
// Manager bu qaydaları çağırır ki, biznes məntiqi bir yerdə, təmiz və təkrar istifadə oluna bilən qalsın.
public class CategoryBusinessRules
{
    private readonly ICategoryRepository _categoryRepository;
    private readonly IProductRepository _productRepository;

    // Yoxlamalar üçün bazaya baxmaq lazımdır, ona görə repository-lər DI ilə gəlir.
    public CategoryBusinessRules(
        ICategoryRepository categoryRepository,
        IProductRepository productRepository)
    {
        _categoryRepository = categoryRepository;
        _productRepository = productRepository;
    }

    // Slug (URL adı) unikal olmalıdır — eyni slug-lu başqa kateqoriya varsa xəta.
    // excludeId: yeniləmədə kateqoriyanın özünü istisna etmək üçün (öz slug-u ilə toqquşmasın).
    public async Task SlugMustBeUniqueAsync(string slug, int? excludeId = null, CancellationToken ct = default)
    {
        bool exists = await _categoryRepository.AnyAsync(
            c => c.Slug == slug && (!excludeId.HasValue || c.Id != excludeId.Value),
            enableTracking: false,
            cancellationToken: ct);

        if (exists)
            throw new BusinessException(Messages.Category.SlugAlreadyExists);
    }

    // Kateqoriya mövcud olmalıdır — yoxdursa NotFound. Tapılarsa obyekti qaytarır.
    public async Task<Category> CategoryMustExistAsync(int id, CancellationToken ct = default)
    {
        Category? category = await _categoryRepository.GetAsync(
            c => c.Id == id,
            enableTracking: false,
            cancellationToken: ct);

        if (category is null)
            throw new NotFoundException(nameof(Category), id);

        return category;
    }

    // Valideyn kateqoriya göstərilibsə, o, bazada mövcud olmalıdır.
    public async Task ParentMustExistIfSpecifiedAsync(int? parentId, CancellationToken ct = default)
    {
        if (!parentId.HasValue)
            return;   // valideyn göstərilməyibsə yoxlamağa ehtiyac yoxdur

        bool exists = await _categoryRepository.AnyAsync(
            c => c.Id == parentId.Value,
            enableTracking: false,
            cancellationToken: ct);

        if (!exists)
            throw new BusinessException(Messages.Category.ParentNotFound);
    }

    // Kateqoriya öz-özünün valideyni ola bilməz (A-nın valideyni yenə A olmaz).
    public void CannotBeOwnParent(int categoryId, int? parentId)
    {
        if (parentId.HasValue && parentId.Value == categoryId)
            throw new BusinessException(Messages.Category.CannotBeOwnParent);
    }

    // DÖVRİ İSTİNADIN qarşısını alır: yeni valideyndən başlayıb zəncirlə yuxarı qalxır;
    // əgər yolda bu kateqoriyanın özünə rast gəlsə, bu təyinat dövr (loop) yaradardı → xəta.
    // safetyHops: sonsuz dövrəyə qarşı təhlükəsizlik limiti (maks. 100 addım).
    public async Task NoCircularReferenceAsync(int categoryId, int? newParentId, CancellationToken ct = default)
    {
        if (!newParentId.HasValue)
            return;

        int? current = newParentId;
        int safetyHops = 0;

        while (current.HasValue && safetyHops++ < 100)
        {
            if (current.Value == categoryId)
                throw new BusinessException(Messages.Category.CircularReference);

            int currentValue = current.Value;
            Category? node = await _categoryRepository.GetAsync(
                c => c.Id == currentValue,
                enableTracking: false,
                cancellationToken: ct);

            current = node?.ParentCategoryId;   // bir pillə yuxarı qalx
        }
    }

    // Alt-kateqoriyası olan kateqoriya silinə bilməz (əvvəl uşaq kateqoriyaları köçür/sil).
    public async Task CannotDeleteWhenHasChildrenAsync(int categoryId, CancellationToken ct = default)
    {
        bool hasChildren = await _categoryRepository.AnyAsync(
            c => c.ParentCategoryId == categoryId,
            enableTracking: false,
            cancellationToken: ct);

        if (hasChildren)
            throw new BusinessException(Messages.Category.HasChildren);
    }

    // İçində məhsulu olan kateqoriya silinə bilməz (məhsullar sahibsiz/kateqoriyasız qalmasın).
    public async Task CannotDeleteWhenHasProductsAsync(int categoryId, CancellationToken ct = default)
    {
        bool hasProducts = await _productRepository.AnyAsync(
            p => p.CategoryId == categoryId,
            enableTracking: false,
            cancellationToken: ct);

        if (hasProducts)
            throw new BusinessException(Messages.Category.HasProducts);
    }
}
