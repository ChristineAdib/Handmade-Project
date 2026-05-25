using HandoraApplication.DTOs.NotificationsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface INotificationHubContext
    {
        Task SendNotificationAsync(string userId, NotificationDto notification);
        Task SendUnreadCountAsync(string userId, int count);
    }
}
