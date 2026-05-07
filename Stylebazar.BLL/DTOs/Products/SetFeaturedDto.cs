namespace Stylebazar.BLL.DTOs.Products;

public class SetFeaturedDto
{
    /// <summary>
    /// How many days the product stays featured. Null = no expiry.
    /// </summary>
    public int? Days { get; set; }
}
