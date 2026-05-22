using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraDomain.Models.ProductEntities
{
    public class Category:BaseEntity<Guid>
    {
        // [LOCALIZATION] store both Arabic & English names directly
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;

        public string? ImageUrl { get; set; } // [IMPROVEMENT] categories usually have an image/icon

        // Self-referencing FK for subcategories
        public Guid? ParentId { get; set; }
        public Category? Parent { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();

        // Navigation
        public ICollection<Product> Products { get; set; } = new List<Product>();
    }
}
