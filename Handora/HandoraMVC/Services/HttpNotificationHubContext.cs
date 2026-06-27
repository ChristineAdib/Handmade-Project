using HandoraApplication.DTOs.NotificationsDto;
using HandoraApplication.IServices;
using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace HandoraMVC.Services
{
    public sealed class HttpNotificationHubContext : INotificationHubContext
    {
        private readonly HttpClient _httpClient;

        public HttpNotificationHubContext(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task SendNotificationAsync(string userId, NotificationDto notification)
        {
            try
            {
                var payload = new
                {
                    UserId = userId,
                    Notification = notification
                };

                // Post to API push-realtime endpoint
                await _httpClient.PostAsJsonAsync("api/Notification/push-realtime", payload);
            }
            catch (Exception)
            {
                // Silence exception to avoid blocking the caller if the API is down
            }
        }

        public Task SendUnreadCountAsync(string userId, int count)
        {
            // API side already fetches the unread count in database and broadcasts UnreadCountUpdated event,
            // so we don't need to post it separately.
            return Task.CompletedTask;
        }
    }
}
