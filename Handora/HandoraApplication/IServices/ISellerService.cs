using HandoraApplication.DTOs.SellerDTOs;
using HandoraApplication.Helpers;

namespace HandoraApplication.IServices
{
    public interface ISellerService
    {
        Task<Result<SellerProfileDto>> GetSellerProfile(string sellerId);
        Task<Result<SellerProfileDto>> GetMyProfile(string sellerId);
        Task<Result<SellerProfileDto>> UpdateMyProfile(string sellerId, UpdateSellerDto dto);
        Task<Result<IEnumerable<SellerProfileDto>>> GetAllSellers();
        Task<Result<SellerProfileDto>> ApproveSeller(string sellerId);
        Task<Result<SellerProfileDto>> SuspendSeller(string sellerId);
        Task<Result<SellerProfileDto>> UnsuspendSeller(string sellerId);


    }
}