using System;
using HandoraDomain.Consts;

namespace HandoraDomain.Models.CustomStudioEntities
{
    public class CustomConfiguration : BaseEntity<Guid>
    {
        public ProductType ProductType { get; set; }
        public string ConfigurationDataJson { get; set; } = string.Empty;

        // Relationship
        public Guid CustomRequestId { get; set; }
        public CustomRequest CustomRequest { get; set; } = null!;
    }
}
