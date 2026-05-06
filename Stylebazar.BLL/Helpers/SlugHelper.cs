using System.Text;
using System.Text.RegularExpressions;

namespace Stylebazar.BLL.Helpers;

public static class SlugHelper
{
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

    private static readonly Regex InvalidChars = new(@"[^a-z0-9\s-]", RegexOptions.Compiled);
    private static readonly Regex MultipleDashes = new(@"-+", RegexOptions.Compiled);
    private static readonly Regex Whitespace = new(@"\s+", RegexOptions.Compiled);

    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        StringBuilder sb = new(input.Length);
        foreach (char c in input)
        {
            if (AzMap.TryGetValue(c, out string? mapped))
                sb.Append(mapped);
            else
                sb.Append(char.ToLowerInvariant(c));
        }

        string slug = sb.ToString();
        slug = Whitespace.Replace(slug, "-");
        slug = InvalidChars.Replace(slug, string.Empty);
        slug = MultipleDashes.Replace(slug, "-");
        return slug.Trim('-');
    }
}
