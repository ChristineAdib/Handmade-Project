using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.FollowDTOs;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.DTOs.ProfileDTOs;
using HandoraApplication.IServices;
using HandoraDomain.Models.AppUser;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public class ProfileService : IProfileService
    {
        private readonly UserManager<User> _userManager;
        private readonly IFollowService _followService;
        private readonly IOrderService _orderService;

        public ProfileService(
            UserManager<User> userManager,
            IFollowService followService,
            IOrderService orderService)
        {
            _userManager = userManager;
            _followService = followService;
            _orderService = orderService;
        }

        public async Task<ProfileDto> GetProfileAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId)
                ?? throw new Exception("User not found");

            return new ProfileDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email!,
                PhoneNumber = user.PhoneNumber,
                Bio = user.Bio,
                ProfileImage = user.ProfileImage,
                CreatedAt = user.CreatedAt
            };
        }

        public async Task<IEnumerable<FollowDto>> GetFollowedShopsAsync(string userId)
        {
            var result = await _followService.GetFollowedShops(userId);
            return result.Data!;
        }

        public async Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(string userId, OrderQueryDto query)
        {
            var result = await _orderService.GetUserOrders(userId, query);
            return result.Data!;
        }
    }
}
