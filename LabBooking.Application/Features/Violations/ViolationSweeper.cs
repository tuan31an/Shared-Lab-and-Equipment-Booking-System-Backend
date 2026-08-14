using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using Microsoft.Extensions.Configuration;

namespace LabBooking.Application.Features.Violations
{
    /// <summary>
    /// Quét định kỳ: (1) ghi nhận vi phạm no-show cho các booking đã duyệt nhưng
    /// không check-in; (2) tự áp dụng hạn chế đặt lịch khi số vi phạm trong cửa sổ
    /// vượt ngưỡng; (3) đồng bộ User.Status với Restriction đang hiệu lực.
    /// </summary>
    public static class ViolationSweeper
    {
        public static async Task<int> SweepAsync(
            IRepository<Booking> bookings,
            IRepository<CheckInOut> checkInOuts,
            IRepository<Violation> violations,
            IRepository<Restriction> restrictions,
            IRepository<User> users,
            IUnitOfWork uow,
            IConfiguration configuration,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;

            var noShowGraceMinutes = double.TryParse(configuration["Violation:NoShowGraceMinutes"], out var noShowGrace) ? noShowGrace : 30;
            var cutOff = now.AddMinutes(-noShowGraceMinutes);

            var dueBookings = await bookings.ListAsync(
                b => b.Status == BookingStatus.Approved && b.EndTime <= cutOff,
                cancellationToken);

            var newlyNoShow = new List<Booking>();
            if (dueBookings.Count > 0)
            {
                var checkedIn = (await checkInOuts.ListAsync(c => c.CheckInTime != null, cancellationToken))
                    .Select(c => c.BookingId)
                    .ToHashSet();
                var recorded = (await violations.ListAsync(v => v.Type == ViolationType.NoShow, cancellationToken))
                    .Select(v => v.BookingId)
                    .Where(id => id.HasValue)
                    .Select(id => id!.Value)
                    .ToHashSet();

                newlyNoShow = dueBookings
                    .Where(b => !checkedIn.Contains(b.Id) && !recorded.Contains(b.Id))
                    .ToList();
            }

            foreach (var b in newlyNoShow)
            {
                await violations.AddAsync(new Violation
                {
                    UserId = b.RequesterId,
                    BookingId = b.Id,
                    Type = ViolationType.NoShow,
                    RecordedAt = now,
                    Note = "Did not check in for the booked slot."
                }, cancellationToken);
            }

            if (newlyNoShow.Count > 0)
                await AutoRestrictAsync(users, violations, restrictions, uow, configuration, newlyNoShow, now, cancellationToken);

            await SyncUserStatusAsync(users, restrictions, uow, now, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            return newlyNoShow.Count;
        }

        private static async Task AutoRestrictAsync(
            IRepository<User> users,
            IRepository<Violation> violations,
            IRepository<Restriction> restrictions,
            IUnitOfWork uow,
            IConfiguration configuration,
            List<Booking> newlyNoShow,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var threshold = int.TryParse(configuration["Violation:Threshold"], out var t) ? t : 3;
            var windowDays = int.TryParse(configuration["Violation:WindowDays"], out var wd) ? wd : 30;
            var restrictionDays = int.TryParse(configuration["Violation:RestrictionDays"], out var rd) ? rd : 7;

            var offenderIds = newlyNoShow.Select(b => b.RequesterId).Distinct().ToList();
            var allViolations = await violations.ListAsync(
                v => offenderIds.Contains(v.UserId) && v.RecordedAt >= now.AddDays(-windowDays),
                cancellationToken);
            var activeRestrictions = await restrictions.ListAsync(
                r => offenderIds.Contains(r.UserId) && r.EndDate >= now.Date && r.StartDate <= now.Date,
                cancellationToken);

            var offenders = await users.ListAsync(u => offenderIds.Contains(u.Id), cancellationToken);
            var offendersById = offenders.ToDictionary(u => u.Id);

            foreach (var offenderId in offenderIds)
            {
                if (allViolations.Count(v => v.UserId == offenderId) >= threshold &&
                    !activeRestrictions.Any(r => r.UserId == offenderId))
                {
                    await restrictions.AddAsync(new Restriction
                    {
                        UserId = offenderId,
                        StartDate = now.Date,
                        EndDate = now.Date.AddDays(restrictionDays),
                        Reason = $"Automatic restriction: {threshold}+ violations in {windowDays} days."
                    }, cancellationToken);

                    if (offendersById.TryGetValue(offenderId, out var user) && user.Status != UserStatus.Restricted)
                    {
                        user.Status = UserStatus.Restricted;
                        users.Update(user);
                    }
                }
            }
        }

        private static async Task SyncUserStatusAsync(
            IRepository<User> users,
            IRepository<Restriction> restrictions,
            IUnitOfWork uow,
            DateTime now,
            CancellationToken cancellationToken)
        {
            var restricted = await restrictions.ListAsync(
                r => r.EndDate >= now.Date && r.StartDate <= now.Date,
                cancellationToken);
            var restrictedUserIds = restricted.Select(r => r.UserId).ToHashSet();

            var allUsers = await users.ListAsync(null, cancellationToken);
            foreach (var user in allUsers)
            {
                var shouldRestrict = restrictedUserIds.Contains(user.Id);
                if (shouldRestrict && user.Status != UserStatus.Restricted)
                {
                    user.Status = UserStatus.Restricted;
                    users.Update(user);
                }
                else if (!shouldRestrict && user.Status == UserStatus.Restricted)
                {
                    user.Status = UserStatus.Active;
                    users.Update(user);
                }
            }
        }
    }
}