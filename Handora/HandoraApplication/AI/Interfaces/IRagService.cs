using HandoraApplication.AI.DTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.AI.Interfaces
{
    public interface IRagService
    {
        Task IndexAsync(RagDocumentDto document);

        Task<IReadOnlyList<RagSearchResultDto>> SearchAsync(
            RagSearchRequestDto request);

        Task DeleteAsync(string collection, string id);
    }
}
