using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using SeasonsCare.Api.Data;
using SeasonsCare.Api.DTOs.Common;
using SeasonsCare.Api.Models.Entities;

namespace SeasonsCare.Api.Repositories
{
    public class NotificationRepository : INotificationRepository
    {
        private readonly ApplicationDbContext _context;

        public NotificationRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddRangeAsync(IEnumerable<Notification> notifications)
        {
            await _context.Notifications.AddRangeAsync(notifications);
        }

        public async Task<(List<Notification> Data, int TotalCount)> GetPagedByRecipientAsync(Guid careGroupId, Guid recipientUserId, PaginationRequest request)
        {
            var query = _context.Notifications
                .Where(x => x.CareGroupId == careGroupId && x.RecipientUserId == recipientUserId);

            var totalCount = await query.CountAsync();

            query = request.Sort switch
            {
                "createdAt_asc" => query.OrderBy(x => x.CreatedAt),
                "isRead_asc" => query.OrderBy(x => x.IsRead).ThenByDescending(x => x.CreatedAt),
                "isRead_desc" => query.OrderByDescending(x => x.IsRead).ThenByDescending(x => x.CreatedAt),
                _ => query.OrderByDescending(x => x.CreatedAt)
            };

            var data = await query
                .Skip((request.Page - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            return (data, totalCount);
        }

        public async Task<Notification?> GetByIdAsync(Guid careGroupId, Guid recipientUserId, Guid notificationId)
        {
            return await _context.Notifications.FirstOrDefaultAsync(x =>
                x.CareGroupId == careGroupId &&
                x.RecipientUserId == recipientUserId &&
                x.Id == notificationId);
        }

        public async Task<List<Notification>> GetUnreadByRecipientAsync(Guid careGroupId, Guid recipientUserId)
        {
            return await _context.Notifications
                .Where(x => x.CareGroupId == careGroupId && x.RecipientUserId == recipientUserId && !x.IsRead)
                .OrderByDescending(x => x.CreatedAt)
                .ToListAsync();
        }

        public async Task<int> GetUnreadCountAsync(Guid careGroupId, Guid recipientUserId)
        {
            return await _context.Notifications.CountAsync(x =>
                x.CareGroupId == careGroupId &&
                x.RecipientUserId == recipientUserId &&
                !x.IsRead);
        }

        public async Task UpdateAsync(Notification notification)
        {
            _context.Notifications.Update(notification);
            await Task.CompletedTask;
        }

        public async Task SaveChangesAsync()
        {
            await _context.SaveChangesAsync();
        }
    }
}
