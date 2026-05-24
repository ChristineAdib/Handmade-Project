using HandoraApplication.DTOs.FollowDTOs;
using HandoraApplication.Helpers;
using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using HandoraDomain.Models.AppUser;
using HandoraDomain.Models.FollowEntities;
using HandoraDomain.Models.ShopEntities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HandoraApplication.Services
{
    public class FollowService(IUnitOfWork unitOfWork, UserManager<User> userManager) : IFollowService
    {
        private readonly IUnitOfWork _uow = unitOfWork;
        private readonly UserManager<User> _userManager = userManager;

        public async Task<Result> FollowShop(string userId, Guid shopId)
        {
            var followRepo = _uow.Repository<Follow, Guid>();
            var query = await followRepo.GetAllAsNoTracking();

            var alreadyFollowing = await query
                .AnyAsync(f => f.UserId == userId && f.ShopId == shopId);
            if (alreadyFollowing)
                return Result.Failure("You are already following this shop");

            var shopRepo = _uow.Repository<Shop, Guid>();
            var shopExists = await (await shopRepo.GetAllAsNoTracking())
                .AnyAsync(s => s.Id == shopId && !s.IsDeleted);
            if (!shopExists)
                return Result.Failure("Shop not found");

            var follow = new Follow
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                ShopId = shopId,
                FollowedAt = DateTime.UtcNow
            };

            await followRepo.AddAsync(follow);
            await _uow.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result> UnfollowShop(string userId, Guid shopId)
        {
            var followRepo = _uow.Repository<Follow, Guid>();
            var query = await followRepo.GetAllAsync();

            var follow = await query
                .FirstOrDefaultAsync(f => f.UserId == userId && f.ShopId == shopId);
            if (follow is null)
                return Result.Failure("You are not following this shop");

            await followRepo.HardDeleteAsync(follow);  
            await _uow.SaveChangesAsync();
            return Result.Success();
        }

        public async Task<Result<bool>> IsFollowing(string userId, Guid shopId)
        {
            var followRepo = _uow.Repository<Follow, Guid>();
            var query = await followRepo.GetAllAsNoTracking();

            var isFollowing = await query
                .AnyAsync(f => f.UserId == userId && f.ShopId == shopId);
            return Result<bool>.Success(isFollowing);
        }

        public async Task<Result<IEnumerable<FollowDto>>> GetFollowedShops(string userId)
        {
            var followRepo = _uow.Repository<Follow, Guid>();
            var query = await followRepo.GetAllAsNoTracking();

            var follows = await query
                .Where(f => f.UserId == userId)
                .Include(f => f.Shop)
                .ToListAsync();

            var result = follows.Select(f => new FollowDto
            {
                ShopId = f.ShopId,
                ShopName = f.Shop.Name,
                ShopLogo = f.Shop.Logo,
                Rating = f.Shop.Rating,
                IsVerified = f.Shop.IsVerified,
                FollowedAt = f.FollowedAt
            });

            return Result<IEnumerable<FollowDto>>.Success(result);
        }

        public async Task<Result<IEnumerable<ShopFollowerDto>>> GetShopFollowers(Guid shopId)
        {
            var followRepo = _uow.Repository<Follow, Guid>();
            var query = await followRepo.GetAllAsNoTracking();

            var follows = await query
                .Where(f => f.ShopId == shopId)
                .ToListAsync();

            var result = new List<ShopFollowerDto>();
            foreach (var f in follows)
            {
                var user = await _userManager.FindByIdAsync(f.UserId);
                if (user is null) continue;
                result.Add(new ShopFollowerDto
                {
                    UserId = user.Id,
                    UserName = user.Name,
                    ProfileImage = user.ProfileImage,
                    FollowedAt = f.FollowedAt
                });
            }

            return Result<IEnumerable<ShopFollowerDto>>.Success(result);
        }
    }
}