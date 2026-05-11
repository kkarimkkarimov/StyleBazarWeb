using Core.Persistence.Repositories;
using Stylebazar.DAL.Entities.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stylebazar.DAL.Entities
{
    public class Product : Entity<int>
    {
        public required string Name { get; set; }
        public required string Slug { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public decimal? DiscountPrice { get; set; }
        public int StockQuantity { get; set; }
        public string? Brand { get; set; }
        public ProductCondition Condition { get; set; } = ProductCondition.New;
        public int StoreId { get; set; }
        public int CategoryId { get; set; }
        public bool IsFeatured { get; set; }
        public DateTime? FeaturedUntil { get; set; }
        public bool IsActive { get; set; }

        public virtual Store Store { get; set; } = null!;
        public virtual Category Category { get; set; } = null!;
        public virtual ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
        public virtual ICollection<ProductSize> Sizes { get; set; } = new List<ProductSize>();
        public virtual ICollection<Wishlist> Wishlists { get; set; } = new List<Wishlist>();
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();
    }
}


