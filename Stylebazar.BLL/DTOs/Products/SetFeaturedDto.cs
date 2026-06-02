namespace Stylebazar.BLL.DTOs.Products;

// SetFeaturedDto — məlumatı UI / API cavabına ötürən DTO (oxu modeli).
public class SetFeaturedDto
{
    /// <summary>
    /// How many days the product stays featured. Null = no expiry.
    /// </summary>
    public int? Days { get; set; }
}
