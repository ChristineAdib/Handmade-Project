using HandoraApplication.DTOs.ProductAgentDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IProductAgentService
    {
        Task<ProductAnalysisResult> AnalyzeProductImageAsync(string imageBase64, string mimeType);
    }
}
