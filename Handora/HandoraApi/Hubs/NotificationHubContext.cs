using HandoraApi.Hubs;
using HandoraApplication.DTOs.NotificationsDto;
using HandoraApplication.IServices;
using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HandoraApplication.Services
{
    public sealed class NotificationHubContext : INotificationHubContext
    {
        private readonly IHubContext<NotificationHub, INotificationHub> _hubContext;

        public NotificationHubContext(IHubContext<NotificationHub, INotificationHub> hubContext)
            => _hubContext = hubContext;

        public Task SendNotificationAsync(string userId, NotificationDto notification)
            => _hubContext.Clients.Group(userId).ReceiveNotification(notification);

        public Task SendUnreadCountAsync(string userId, int count)
            => _hubContext.Clients.Group(userId).UnreadCountUpdated(count);
    }
}
