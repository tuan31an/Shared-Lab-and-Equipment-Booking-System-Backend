using LabBooking.Infrastructure.Sqlserver.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LabBooking.API.Common
{
    /// <summary>
    /// Dọn định kỳ refresh token đã hết hạn hoặc đã thu hồi.
    /// </summary>
    public class RefreshTokenCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly TimeSpan _interval;

        public RefreshTokenCleanupService(IServiceScopeFactory scopeFactory, IConfiguration configuration)
        {
            _scopeFactory = scopeFactory;
            var hours = configuration.GetValue<double?>("RefreshTokenCleanup:IntervalHours") ?? 1;
            _interval = TimeSpan.FromHours(hours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                await db.RefreshTokens
                    .Where(rt => rt.ExpiresAtUtc <= DateTime.UtcNow || rt.RevokedAtUtc != null)
                    .ExecuteDeleteAsync(stoppingToken);
            }
        }
    }
}