using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.Category_TagDTOs
{
    public class CategorySummaryDto
    {
        public Guid Id { get; set; }
        public string NameEn { get; set; } = string.Empty;
        public string NameAr { get; set; } = string.Empty;
    }
}
