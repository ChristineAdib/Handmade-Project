using HandoraApplication.IServices;
using HandoraDomain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Text.RegularExpressions;

namespace HandoraApi.Hubs
{
    [Authorize]
    public sealed class NotificationHub : Hub<INotificationHub>
    {
        private readonly INotificationRepository _repo;

        public NotificationHub(INotificationRepository repo)
            => _repo = repo;

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier!;
            await Groups.AddToGroupAsync(Context.ConnectionId, userId);

            var count = await _repo.GetUnreadCountAsync(userId);
            await Clients.Caller.UnreadCountUpdated(count);

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = Context.UserIdentifier!;
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, userId);
            await base.OnDisconnectedAsync(exception);
        }
    }
}
