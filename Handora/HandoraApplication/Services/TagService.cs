using HandoraApplication.DTOs.Category_TagDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class TagService(IUnitOfWork unitOfWork) : ITagService
    {
        private readonly IUnitOfWork _unitOfWork = unitOfWork;

        public async Task<Result<IEnumerable<TagDto>>> GetAllTags()
        {
            var repo = _unitOfWork.Repository<Tag, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var tags = await query.ToListAsync();

            var result = tags.Select(t => new TagDto
            {
                Id = t.Id,
                Name = t.Name
            });

            return Result<IEnumerable<TagDto>>.Success(result);
        }

        public async Task<Result<TagDto>> GetTagById(Guid id)
        {
            var repo = _unitOfWork.Repository<Tag, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var tag = await query.FirstOrDefaultAsync(t => t.Id == id);

            if (tag is null)
                return Result<TagDto>.Failure("Tag not found");

            var dto = new TagDto
            {
                Id = tag.Id,
                Name = tag.Name
            };

            return Result<TagDto>.Success(dto);
        }
    }
}
