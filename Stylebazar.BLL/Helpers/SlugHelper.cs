using System.Text;
using System.Text.RegularExpressions;

namespace Stylebazar.BLL.Helpers;

// SLUG KÖMƏKÇİSİ — adı URL üçün təmiz "slug"-a çevirir.
// Məs. "Qırmızı Köynək" → "qirmizi-koynek". Azərbaycan hərflərini latın qarşılığına salır.
public static class SlugHelper
{
    // Azərbaycan hərfləri → ASCII qarşılıqları (URL-də xüsusi hərf olmasın deyə).
    private static readonly Dictionary<char, string> AzMap = new()
    {
        ['ə'] = "e", ['Ə'] = "e",
        ['ı'] = "i", ['I'] = "i",
        ['İ'] = "i",
        ['ç'] = "c", ['Ç'] = "c",
        ['ş'] = "s", ['Ş'] = "s",
        ['ğ'] = "g", ['Ğ'] = "g",
        ['ö'] = "o", ['Ö'] = "o",
        ['ü'] = "u", ['Ü'] = "u"
    };

    // Əvvəlcədən hazırlanmış (compiled) regex-lər — sürət üçün.
    private static readonly Regex InvalidChars = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);   // icazəsiz simvollar
    private static readonly Regex MultipleDashes = new(@"-+", RegexOptions.Compiled);            // ardıcıl tirelər
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);               // boşluqlar

    // Mətni slug-a çevirən əsas metod.
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        // 1) Hər hərfi kiçilt; Azərbaycan hərfini ASCII-yə çevir.
        StringBuilder sb = new(input.Length);
        foreach (char c in input)
        {
            if (AzMap.TryGetValue(c, out string? mapped))
                sb.Append(mapped);
            else
                sb.Append(char.ToLowerInvariant(c));
        }

        // 2) Boşluqları tireyə çevir, icazəsiz simvolları sil, ardıcıl tireləri birləşdir.
        string slug = sb.ToString();
        slug = Whitespace.Replace(slug, "-");
        slug = InvalidChars.Replace(slug, string.Empty);
        slug = MultipleDashes.Replace(slug, "-");
        return slug.Trim('-');   // baş/son tireləri at
    }
}
