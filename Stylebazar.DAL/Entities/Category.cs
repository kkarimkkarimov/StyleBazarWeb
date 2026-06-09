// Core.Persistence.Repositories — Entity<TId> baza sinfini gətirir (Id + audit tarixləri).
using Core.Persistence.Repositories;
// Aşağıdakı System ad fəzaları (DateTime, List<>, StringBuilder) şablonla gəlib — saxlanılır.
using System;
using System.Collections.Generic;
using System.Text;

// DAL entity-lərinin ad fəzası.
namespace Stylebazar.DAL.Entities
{
    // KATEQORIYA entity-si — "Categories" cədvəli. Məhsullar kateqoriyalara bölünür.
    // İYERARXİKDİR: bir kateqoriyanın valideyni (ParentCategory) və alt-kateqoriyaları
    // (ChildCategories) ola bilər — yəni ağac (tree) quruluşu yaranır.
    public class Category : Entity<int>
    {
        // Kateqoriya adı (məs. "Qadın geyimi"). Məcburidir.
        public required string Name { get; set; }
        // URL üçün təmiz ad (slug).
        public required string Slug { get; set; }
        // Valideyn kateqoriyanın id-si. null → bu, ən üst (kök) kateqoriyadır.
        public int? ParentCategoryId { get; set; }

        // --- Naviqasiya sahələri (özünə-istinad edən əlaqə) ---
        // Valideyn kateqoriya (kök kateqoriyada bu null olur).
        public virtual Category? ParentCategory { get; set; }
        // Bu kateqoriyanın alt-kateqoriyaları (bir valideyn → çox uşaq).
        public virtual ICollection<Category> ChildCategories { get; set; } = new List<Category>();
        // Bu kateqoriyaya aid məhsullar.
        public virtual ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
