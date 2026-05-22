

namespace HandoraDomain.Models.ProductEntities
{
    public class Tag:BaseEntity<Guid>
    {
        public string Name { get; set; } = string.Empty;

        // Navigation
        public ICollection<Product> Products { get; set; } = [];
    }
}
