using Core.Persistence.Repositories;
using System;
using System.Collections.Generic;
using System.Text;

namespace Stylebazar.DAL.Entities
{
    // KATEQORIYA entity-si — "Categories" cədvəli. Məhsullar kateqoriyalara bölünür.
    // Kateqoriya İYERARXİKDİR: bir kateqoriyanın valideyni (ParentCategory) və
    // alt-kateqoriyaları (ChildCategories) ola bilər — yəni ağac (tree) quruluşu.
    public class Category : Entity<int>
    {
        public required string Name { get; set; }      // Kateqoriya adı (məs. "Qadın geyimi")
        public required string Slug { get; set; }      // URL üçün təmiz ad
        public int? ParentCategoryId { get; set; }     // Valideyn kateqoriyanın id-si; null = ən üst (kök) kateqoriya

        // --- Naviqasiya sahələri (özünə-istinad edən əlaqə) ---
        public virtual Category? ParentCategory { get; set; }                                       // Valideyn kateqoriya
        public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();   // Alt-kateqoriyalar
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();            // Bu kateqoriyadakı məhsullar
    }
}
