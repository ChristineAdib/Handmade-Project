using HandoraApplication.DTOs.ShopDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices;

public interface IShopService
{
    Task<Result<ShopDto>> GetShopById(Guid id);
    Task<Result<ShopDto>> GetShopWithProducts(Guid id);
    Task<Result<ShopDto>> GetMyShop(string ownerId);
    Task<Result<ShopStatsDto>> GetShopStats(Guid id);
    Task<Result<IEnumerable<ShopDto>>> GetTopRatedShops(int count = 10);
    Task<Result<IEnumerable<ShopDto>>> SearchShops(ShopFilterDto filter);
    Task<Result<ShopDto>> CreateShop(string ownerId, CreateShopDto dto);
    Task<Result<ShopDto>> UpdateShop(Guid id, string ownerId, UpdateShopDto dto);
    Task<Result> DeleteShop(Guid id, string ownerId);
}