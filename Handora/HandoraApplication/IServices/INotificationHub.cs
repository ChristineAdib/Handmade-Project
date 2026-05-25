using HandoraApplication.DTOs.NotificationsDto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.IServices
{
    public interface INotificationHub
    {
        Task ReceiveNotification(NotificationDto notification);
        Task UnreadCountUpdated(int count);
    }
}
