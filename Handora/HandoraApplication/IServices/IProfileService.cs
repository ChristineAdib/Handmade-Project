using HandoraApplication.DTOs.Common;
using HandoraApplication.DTOs.FollowDTOs;
using HandoraApplication.DTOs.OrderDTOs;
using HandoraApplication.DTOs.ProfileDTOs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface IProfileService
    {
        Task<ProfileDto> GetProfileAsync(string userId);

        Task<IEnumerable<FollowDto>> GetFollowedShopsAsync(string userId);

        Task<PagedResultDto<OrderSummaryDto>> GetOrdersAsync(string userId, OrderQueryDto query);
    }
}
