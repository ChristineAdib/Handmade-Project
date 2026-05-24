using HandoraApplication.DTOs.FollowDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices
{
    public interface IFollowService
    {
        Task<Result> FollowShop(string userId, Guid shopId);
        Task<Result> UnfollowShop(string userId, Guid shopId);
        Task<Result<bool>> IsFollowing(string userId, Guid shopId);
        Task<Result<IEnumerable<FollowDto>>> GetFollowedShops(string userId);
        Task<Result<IEnumerable<ShopFollowerDto>>> GetShopFollowers(Guid shopId);
    }
}