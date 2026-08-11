using LabBooking.Domain.Enums;
using LabBooking.Infrastructure.Sqlserver.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabBooking.API.Common
{
    /// <summary>
    /// Nhắc lịch tự động: mỗi chu kỳ, tạo thông báo BookingReminder cho các booking
    /// đã duyệt sắp bắt đầu (trong cửa sổ nhắc trước). Nội dung mang prefix định danh
    /// booking để không tạo trùng lần nhắc.
    /// </summary>
    public class BookingReminderService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;

        public BookingReminderService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            var intervalMinutes = configuration.GetValue<double?>("Notification:ReminderIntervalMinutes") ?? 15;
            _interval = TimeSpan.FromMinutes(intervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                var now = DateTime.UtcNow;
                var reminderHours = configuration.GetValue<double?>("Notification:ReminderHours") ?? 1;
                var to = now.AddHours(reminderHours);

                var upcoming = await db.Bookings
                    .Include(b => b.Resource)
                    .Where(b => b.Status == BookingStatus.Approved && b.StartTime > now && b.StartTime <= to)
                    .ToListAsync(stoppingToken);
                if (upcoming.Count == 0)
                    continue;

                // ponytail: substring scan on content for dedupe; swap to a BookingId FK column if scale requires.
                var existing = await db.Notifications
                    .Where(n => n.Type == NotificationType.BookingReminder)
                    .Select(n => n.Content)
                    .ToListAsync(stoppingToken);

                foreach (var b in upcoming)
                {
                    var key = $"BookingReminder:{b.Id}:";
                    if (existing.Any(e => e.StartsWith(key)))
                        continue;

                    db.Notifications.Add(new Domain.Entities.Notification
                    {
                        UserId = b.RequesterId,
                        Type = NotificationType.BookingReminder,
                        Content = $"{key}{b.Resource.Name} starts at {b.StartTime:u}"
                    });
                    existing.Add(key);
                }

                await db.SaveChangesAsync(stoppingToken);
            }
        }
    }
}