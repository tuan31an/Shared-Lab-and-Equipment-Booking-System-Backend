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
        private readonly ILogger<RefreshTokenCleanupService> _logger;
        private readonly TimeSpan _interval;

        public RefreshTokenCleanupService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RefreshTokenCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var hours = configuration.GetValue<double?>("RefreshTokenCleanup:IntervalHours") ?? 1;
            if (!double.IsFinite(hours) || hours <= 0 || hours > 24)
                throw new InvalidOperationException("RefreshTokenCleanup:IntervalHours must be greater than 0 and at most 24.");

            _interval = TimeSpan.FromHours(hours);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                    await db.RefreshTokens
                        .Where(rt => rt.ExpiresAtUtc <= DateTime.UtcNow || rt.RevokedAtUtc != null)
                        .ExecuteDeleteAsync(stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Failed to clean up expired refresh tokens.");
                }
            }
        }
    }
}
