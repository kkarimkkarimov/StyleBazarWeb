namespace Core.Persistence.Paging;

// Səhifələnmiş (paging) nəticəni saxlayan ümumi konteyner.
// Məsələn: 1000 məhsuldan yalnız 10-nu göstərib "növbəti səhifə" düyməsi qurmaq üçün
// lazım olan bütün məlumatı bir yerdə saxlayır.
public class Paginate<T>
{
    public int Index { get; set; }       // Cari səhifənin nömrəsi (0-dan başlayır)
    public int Size { get; set; }        // Səhifədə neçə element var
    public int Count { get; set; }       // ÜMUMİ element sayı (bütün səhifələrdə)
    public int Pages { get; set; }       // Ümumi səhifə sayı
    public IList<T> Items { get; set; }  // Yalnız BU səhifədəki elementlər

    // Əvvəlki səhifə varmı? (birinci səhifədə deyiliksə true)
    public bool HasPrevious => Index > 0;

    // Növbəti səhifə varmı? (sonuncu səhifədə deyiliksə true)
    public bool HasNext => Index + 1 < Pages;

    public Paginate()
    {
        // Boş massivlə başladırıq ki, Items heç vaxt null olmasın (null xətalarından qoruyur).
        Items = Array.Empty<T>();
    }
}
