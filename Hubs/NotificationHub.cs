using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace SeasonsCare.Api.Hubs
{
    [Authorize]
    public class NotificationHub : Hub
    {
        public const string HubRoute = "/hubs/notifications";
        public const string NotificationReceivedMethod = "notificationReceived";
        public const string UnreadCountUpdatedMethod = "notificationUnreadCountUpdated";

        public override async Task OnConnectedAsync()
        {
            var userId = ResolveUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
            }

            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = ResolveUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, GetUserGroup(userId.Value));
            }

            await base.OnDisconnectedAsync(exception);
        }

        public static string GetUserGroup(Guid userId) => $"user:{userId}";

        private Guid? ResolveUserId()
        {
            var userIdString = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? Context.User?.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdString, out var userId) ? userId : null;
        }
    }
}
