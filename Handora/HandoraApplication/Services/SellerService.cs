using HandoraApplication.DTOs.SellerDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.ShopEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services
{
    public class SellerService(IUnitOfWork unitOfWork, UserManager<User> userManager, IFileService fileService) : ISellerService
    {
        private readonly IUnitOfWork _uow = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;
        private readonly IFileService _fileService = fileService;

        private static SellerProfileDto ToDto(User user, Shop? shop) => new()
        {
            Id = user.Id,
            Name = user.Name,
            Bio = user.Bio,
            ProfileImage = user.ProfileImage,
            MemberSince = user.CreatedAt,
            ShopId = shop?.Id ?? Guid.Empty,
            ShopName = shop?.Name ?? string.Empty,
            Rating = shop?.Rating ?? 0,
            ReviewCount = shop?.ReviewCount ?? 0,
            IsVerified = shop?.IsVerified ?? false
        };

        public async Task<Result<SellerProfileDto>> GetSellerProfile(string sellerId)
        {
            var user = await _userManager.FindByIdAsync(sellerId);

            if (user is null || user.IsDeleted)
                return Result<SellerProfileDto>.Failure("Seller not found");

            var shopRepo = _uow.Repository<Shop, Guid>();
            var shop = await (await shopRepo.GetAllAsNoTracking())
                .FirstOrDefaultAsync(s => s.OwnerId == sellerId && !s.IsDeleted);

            return Result<SellerProfileDto>.Success(ToDto(user, shop));
        }

        public async Task<Result<SellerProfileDto>> GetMyProfile(string sellerId)
            => await GetSellerProfile(sellerId);

        public async Task<Result<SellerProfileDto>> UpdateMyProfile(string sellerId, UpdateSellerDto dto)
        {
            var user = await _userManager.FindByIdAsync(sellerId);

            if (user is null || user.IsDeleted)
                return Result<SellerProfileDto>.Failure("Seller not found");

            if (dto.Name is not null) user.Name = dto.Name;
            if (dto.Bio is not null) user.Bio = dto.Bio;
            if (dto.ProfileImage is not null)
            {
                if (user.ProfileImage is not null)
                    await _fileService.DeleteFileAsync(user.ProfileImage);
                user.ProfileImage = await _fileService.UploadFileAsync(dto.ProfileImage, "profiles");
            }
            user.UpdatedAt = DateTime.UtcNow;
            user.UpdatedBy = sellerId;

            await _userManager.UpdateAsync(user);

            var shopRepo = _uow.Repository<Shop, Guid>();
            var shop = await (await shopRepo.GetAllAsNoTracking())
                .FirstOrDefaultAsync(s => s.OwnerId == sellerId && !s.IsDeleted);

            return Result<SellerProfileDto>.Success(ToDto(user, shop));
        }

        public async Task<Result<IEnumerable<SellerProfileDto>>> GetAllSellers()
        {
            var users = await _userManager.GetUsersInRoleAsync("Seller");
            var shopRepo = _uow.Repository<Shop, Guid>();
            var shopsQuery = await shopRepo.GetAllAsNoTracking();
            var shops = await shopsQuery.Where(s => !s.IsDeleted).ToListAsync();
            
            var result = users
    .Where(u => !u.IsDeleted)
    .Select(u =>
    {
        var shop = shops.FirstOrDefault(s => s.OwnerId == u.Id);
        var dto = ToDto(u, shop);
        dto.IsSuspended = u.IsBanned;
        return dto;
    });
            return Result<IEnumerable<SellerProfileDto>>.Success(result);
        }

        public async Task<Result<SellerProfileDto>> ApproveSeller(string sellerId)
        {
            var shopRepo = _uow.Repository<Shop, Guid>();
            var shop = await (await shopRepo.GetAllAsync())
                .FirstOrDefaultAsync(s => s.OwnerId == sellerId && !s.IsDeleted);

            if (shop is null)
                return Result<SellerProfileDto>.Failure("Shop not found");

            shop.IsVerified = true;
            shop.UpdatedAt = DateTime.UtcNow;

            await shopRepo.UpdateAsync(shop);
            await _uow.SaveChangesAsync();

            var user = await _userManager.FindByIdAsync(sellerId);
            return Result<SellerProfileDto>.Success(ToDto(user!, shop));
        }

        public async Task<Result<SellerProfileDto>> SuspendSeller(string sellerId)
        {
            var user = await _userManager.FindByIdAsync(sellerId);
            if (user is null || user.IsDeleted)
                return Result<SellerProfileDto>.Failure("Seller not found");

            user.IsBanned = true;
            user.IsActive = false;
            await _userManager.UpdateAsync(user);

            var shopRepo = _uow.Repository<Shop, Guid>();
            var shop = await (await shopRepo.GetAllAsNoTracking())
                .FirstOrDefaultAsync(s => s.OwnerId == sellerId && !s.IsDeleted);

            return Result<SellerProfileDto>.Success(ToDto(user, shop));
        }

        public async Task<Result<SellerProfileDto>> UnsuspendSeller(string sellerId)
        {
            var user = await _userManager.FindByIdAsync(sellerId);
            if (user is null || user.IsDeleted)
                return Result<SellerProfileDto>.Failure("Seller not found");

            user.IsBanned = false;
            user.IsActive = true;
            await _userManager.UpdateAsync(user);

            var shopRepo = _uow.Repository<Shop, Guid>();
            var shop = await (await shopRepo.GetAllAsNoTracking())
                .FirstOrDefaultAsync(s => s.OwnerId == sellerId && !s.IsDeleted);

            return Result<SellerProfileDto>.Success(ToDto(user, shop));
        }
    }

}