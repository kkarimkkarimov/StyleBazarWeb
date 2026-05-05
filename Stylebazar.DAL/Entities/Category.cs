using Core.Persistence.Repositories;

namespace Stylebazar.DAL.Entities;

public class Category : Entity<int>
{
    public required string Name { get; set; }
    public required string Slug { get; set; }
    public int? ParentCategoryId { get; set; }

    public virtual Category? ParentCategory { get; set; }
    public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();
    public virtual ICollection<Product> Products { get; set; } = new List<Product>();
}
