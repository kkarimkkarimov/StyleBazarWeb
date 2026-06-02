namespace Stylebazar.BLL.DTOs.Stories;

// CreateStoryDto — yeni qeyd YARATMAQ üçün lazım olan sahələri daşıyan DTO (məlumat ötürmə obyekti).
public class CreateStoryDto
{
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }

    /// <summary>Optional override; defaults to 24h.</summary>
    public int? HoursToExpire { get; set; }

    /// <summary>
    /// Video duration in seconds — null if it's an image. Captured client-side
    /// from the &lt;video&gt; element before upload so the server can validate
    /// against the max-30s rule without server-side ffprobe.
    /// </summary>
    public int? DurationSeconds { get; set; }
}
