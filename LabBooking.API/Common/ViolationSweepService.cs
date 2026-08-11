using LabBooking.Application.Features.Violations;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;

namespace LabBooking.API.Common
{
    /// <summary>
    /// Quét định kỳ để ghi nhận vi phạm no-show và tự động áp dụng hạn chế đặt lịch
    /// khi số vi phạm vượt ngưỡng (logic trong ViolationSweeper).
    /// </summary>
    public class ViolationSweepService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<ViolationSweepService> _logger;
        private readonly TimeSpan _interval;

        public ViolationSweepService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<ViolationSweepService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            var intervalMinutes = configuration.GetValue<double?>("Violation:SweepIntervalMinutes") ?? 15;
            _interval = TimeSpan.FromMinutes(intervalMinutes);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(_interval);

            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                using var scope = _scopeFactory.CreateScope();
                var bookings = scope.ServiceProvider.GetRequiredService<IRepository<Booking>>();
                var checkInOuts = scope.ServiceProvider.GetRequiredService<IRepository<CheckInOut>>();
                var violations = scope.ServiceProvider.GetRequiredService<IRepository<Violation>>();
                var restrictions = scope.ServiceProvider.GetRequiredService<IRepository<Restriction>>();
                var users = scope.ServiceProvider.GetRequiredService<IRepository<User>>();
                var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();

                try
                {
                    // Race: 2 instance chạy đồng thời có thể cùng ghi no-show; unique index
                    // (BookingId, Type) chặn bản trùng — bắt exception để chu kỳ sau quét lại.
                    await ViolationSweeper.SweepAsync(
                        bookings, checkInOuts, violations, restrictions, users, uow, configuration, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Violation sweep failed; will retry next tick.");
                }
            }
        }
    }
}