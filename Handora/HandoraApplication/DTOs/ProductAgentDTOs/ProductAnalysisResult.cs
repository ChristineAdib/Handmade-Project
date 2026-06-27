using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.DTOs.ProductAgentDTOs
{
    public class ProductAnalysisResult
    {
        public string TitleEn { get; set; }
        public string TitleAr { get; set; }
        public string DescriptionEn { get; set; }
        public string DescriptionAr { get; set; }
        public decimal SuggestedPrice { get; set; }
        public string Category { get; set; }
        public List<string> Tags { get; set; }
    }

    public class AnalyzeImageRequest
    {
        public string ImageBase64 { get; set; }
        public string MimeType { get; set; }
    }
}
