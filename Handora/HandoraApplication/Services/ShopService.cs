using HandoraApplication.DTOs.ShopDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.ProductEntities;
using HandoraDomain.Models.ShopEntities;
using Mapster;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services
{
    public class ShopService(IUnitOfWork unitOfWork, IFileService fileService) : IShopService
    {
        private readonly IUnitOfWork _uow = unitOfWork;
        private readonly IFileService _fileService = fileService;

        public async Task<Result<ShopDto>> GetShopById(Guid id)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shop = await query
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            return shop is null
                ? Result<ShopDto>.Failure("Shop not found")
                : Result<ShopDto>.Success(shop.Adapt<ShopDto>());
        }

        public async Task<Result<ShopWithProductsDto>> GetShopWithProducts(Guid id)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shop = await query
                .Include(s => s.Owner)
                .Include(s => s.Products.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Images)
                .Include(s => s.Products.Where(p => !p.IsDeleted))
                    .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            return shop is null
                ? Result<ShopWithProductsDto>.Failure("Shop not found")
                : Result<ShopWithProductsDto>.Success(shop.Adapt<ShopWithProductsDto>());
        }

        public async Task<Result<ShopDto>> GetMyShop(string ownerId)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shop = await query
                .Include(s => s.Owner)
                .FirstOrDefaultAsync(s => s.OwnerId == ownerId && !s.IsDeleted);

            return shop is null
                ? Result<ShopDto>.Failure("You don't have a shop yet")
                : Result<ShopDto>.Success(shop.Adapt<ShopDto>());
        }

        public async Task<Result<ShopStatsDto>> GetShopStats(Guid id)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shop = await query
                .Include(s => s.Products.Where(p => !p.IsDeleted))
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (shop is null)
                return Result<ShopStatsDto>.Failure("Shop not found");

            var stats = new ShopStatsDto
            {
                TotalSales = shop.TotalSales,
                Rating = shop.Rating,
                ReviewCount = shop.ReviewCount,
                ProductCount = shop.Products.Count,
                ActiveProductCount = shop.Products.Count(p => p.Status == ProductStatus.Active)
            };

            return Result<ShopStatsDto>.Success(stats);
        }

        public async Task<Result<IEnumerable<ShopDto>>> GetTopRatedShops(int count = 10)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shops = await query
     .Include(s => s.Owner)
     .Where(s => !s.IsDeleted && s.IsVerified)
     .OrderByDescending(s => s.Rating)
     .ThenByDescending(s => s.ReviewCount)
     .Take(count)
     .ToListAsync();

            return Result<IEnumerable<ShopDto>>.Success(shops.Adapt<IEnumerable<ShopDto>>());
        }

        public async Task<Result<IEnumerable<ShopDto>>> SearchShops(ShopFilterDto filter)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var shops = query
     .Include(s => s.Owner)
     .Where(s => !s.IsDeleted);

            if (!string.IsNullOrEmpty(filter.Search))
                shops = shops.Where(s =>
                    s.Name.Contains(filter.Search) ||
                    (s.DescriptionEn != null && s.DescriptionEn.Contains(filter.Search)) ||
                    (s.DescriptionAr != null && s.DescriptionAr.Contains(filter.Search)));

            if (filter.MinRating.HasValue)
                shops = shops.Where(s => s.Rating >= filter.MinRating.Value);

            if (filter.IsVerified.HasValue)
                shops = shops.Where(s => s.IsVerified == filter.IsVerified.Value);

            shops = filter.SortBy switch
            {
                "rating" => shops.OrderByDescending(s => s.Rating),
                "sales" => shops.OrderByDescending(s => s.TotalSales),
                "newest" => shops.OrderByDescending(s => s.CreatedAt),
                _ => shops.OrderByDescending(s => s.Rating)
            };

            var result = await shops
                .Skip((filter.PageNumber - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync();

            return Result<IEnumerable<ShopDto>>.Success(result.Adapt<IEnumerable<ShopDto>>());
        }

        public async Task<Result<ShopDto>> CreateShop(string ownerId, CreateShopDto dto)
        {
            var repo = _uow.Repository<Shop, Guid>();
            var query = await repo.GetAllAsNoTracking();

            var alreadyHasShop = await query
                .AnyAsync(s => s.OwnerId == ownerId && !s.IsDeleted);
            if (alreadyHasShop)
                return Result<ShopDto>.Failure("You already have a shop");

            var nameTaken = await query
                .AnyAsync(s => s.Name == dto.Name && !s.IsDeleted);
            if (nameTaken)
                return Result<ShopDto>.Failure("Shop name is already taken");

            var logoUrl = dto.Logo is not null
                ? await _fileService.UploadFileAsync(dto.Logo, "shops")
                : null;

            var shop = new Shop
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                DescriptionEn = dto.DescriptionEn,
                DescriptionAr = dto.DescriptionAr,
                Logo = logoUrl,
                OwnerId = ownerId,
                CreatedBy = ownerId
            };

            await repo.AddAsync(shop);
            await _uow.SaveChangesAsync();

            return Result<ShopDto>.Success(shop.Adapt<ShopDto>());
        }

        public async Task<Result<ShopDto>> UpdateShop(Guid id, string ownerId, UpdateShopDto dto)
        {
            var repo = _uow.Repository<Shop, Guid>();

            var shop = await (await repo.GetAllAsync())
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId && !s.IsDeleted);

            if (shop is null)
                return Result<ShopDto>.Failure("Shop not found or access denied");

            if (dto.Name is not null)
            {
                var nameTaken = await (await repo.GetAllAsNoTracking())
                    .AnyAsync(s => s.Name == dto.Name && s.Id != id && !s.IsDeleted);
                if (nameTaken)
                    return Result<ShopDto>.Failure("Shop name is already taken");

                shop.Name = dto.Name;
            }

            if (dto.DescriptionEn is not null) shop.DescriptionEn = dto.DescriptionEn;
            if (dto.DescriptionAr is not null) shop.DescriptionAr = dto.DescriptionAr;
            if (dto.Logo is not null)
            {
                if (shop.Logo is not null)
                    await _fileService.DeleteFileAsync(shop.Logo);
                shop.Logo = await _fileService.UploadFileAsync(dto.Logo, "shops");
            }
            shop.UpdatedAt = DateTime.UtcNow;
            shop.UpdatedBy = ownerId;

            await repo.UpdateAsync(shop);
            await _uow.SaveChangesAsync();

            return Result<ShopDto>.Success(shop.Adapt<ShopDto>());
        }

        public async Task<Result> DeleteShop(Guid id, string ownerId)
        {
            var repo = _uow.Repository<Shop, Guid>();

            var shop = await (await repo.GetAllAsync())
                .FirstOrDefaultAsync(s => s.Id == id && s.OwnerId == ownerId && !s.IsDeleted);

            if (shop is null)
                return Result.Failure("Shop not found or access denied");

            await repo.SoftDeleteAsync(shop);
            await _uow.SaveChangesAsync();
            return Result.Success();
        }
    }
}