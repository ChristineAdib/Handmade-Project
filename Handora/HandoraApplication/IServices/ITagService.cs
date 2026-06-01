using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface ITagService
    {
        Task<Result<IEnumerable<TagDto>>> GetAllTags();
        Task<Result<TagDto>> GetTagById(Guid id);
    }
}
