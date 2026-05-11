namespace Stylebazar.BLL.DTOs.Stories;

public class CreateStoryDto
{
    public string? Caption { get; set; }
    public string? LinkUrl { get; set; }

    /// <summary>Optional override; defaults to 24h.</summary>
    public int? HoursToExpire { get; set; }
}
