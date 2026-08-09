using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using LabBooking.Domain.Scheduling;

namespace LabBooking.Application.Features.Bookings
{
    /// <summary>
    /// Dựng BookingDto + phát hiện xung đột lịch + đề xuất khung thay thế dùng chung
    /// cho CreateBooking và CheckBookingConflict. Booking "đang chiếm giữ" khung giờ:
    /// trạng thái Pending hoặc Approved. Bảo trì chồng lấn cũng chặn khung giờ.
    /// </summary>
    internal static class BookingEvaluation
    {
        public static BookingDto ToDto(
            Booking b,
            IReadOnlyDictionary<Guid, Resource> resources,
            IReadOnlyDictionary<Guid, User> users,
            IReadOnlyDictionary<Guid, PriorityRule> rules)
        {
            resources.TryGetValue(b.ResourceId, out var resource);
            users.TryGetValue(b.RequesterId, out var requester);
            rules.TryGetValue(b.RuleId ?? Guid.Empty, out var rule);

            return new BookingDto(
                b.Id,
                b.ResourceId,
                resource?.Name,
                b.RequesterId,
                requester?.FullName,
                b.RuleId,
                rule?.Name,
                b.StartTime,
                b.EndTime,
                b.Purpose,
                b.Status.ToString(),
                b.ApprovedBy,
                b.ApprovedAt,
                b.CheckInOut?.CheckInTime,
                b.CheckInOut?.CheckOutTime,
                b.CheckInOut?.ActualDuration,
                b.CreatedAt);
        }

        public static IReadOnlyList<BookingDto> ToDtos(
            IEnumerable<Booking> bookings,
            IReadOnlyDictionary<Guid, Resource> resources,
            IReadOnlyDictionary<Guid, User> users,
            IReadOnlyDictionary<Guid, PriorityRule> rules)
            => bookings.Select(b => ToDto(b, resources, users, rules)).ToList();

        /// <summary>Gắn bản ghi CheckInOut (nếu có) vào từng booking trước khi map DTO.</summary>
        public static async Task AttachCheckInsAsync(
            IRepository<CheckInOut> checkInOuts,
            IEnumerable<Booking> bookings,
            CancellationToken cancellationToken)
        {
            var ids = bookings.Select(b => b.Id).ToHashSet();
            if (ids.Count == 0)
                return;

            var map = (await checkInOuts.ListAsync(c => ids.Contains(c.BookingId), cancellationToken))
                .ToDictionary(c => c.BookingId);

            foreach (var b in bookings)
                if (map.TryGetValue(b.Id, out var cio))
                    b.CheckInOut = cio;
        }

        public static bool IsHoldingSlot(BookingStatus status)
            => status is BookingStatus.Pending or BookingStatus.Approved;

        /// <summary>Các booking trùng khung giờ [start, end].</summary>
        public static IEnumerable<Booking> Overlapping(IEnumerable<Booking> bookings, DateTime start, DateTime end)
            => bookings.Where(b => Scheduling.IsOverlap(b.StartTime, b.EndTime, start, end));

        /// <summary>
        /// Toàn bộ khoảng giờ đang bị chiếm giữ trên resource trong cửa sổ
        /// [start - 3 ngày, end + 3 ngày] (để lấp khung thay thế sát trong phạm vi này).
        /// </summary>
        public static IReadOnlyList<(DateTime Start, DateTime End)> BlockedRanges(
            DateTime start,
            DateTime end,
            IEnumerable<Booking> bookings,
            IEnumerable<Maintenance> maintenances)
        {
            var from = start.AddDays(-3);
            var to = end.AddDays(3);

            var ranges = bookings
                .Where(b => IsHoldingSlot(b.Status))
                .Where(b => b.StartTime < to && from < b.EndTime)
                .Select(b => (b.StartTime, b.EndTime));

            var maintenanceRanges = maintenances
                .Where(m => m.Status != MaintenanceStatus.Completed)
                .Where(m => m.StartTime < to && from < m.EndTime)
                .Select(m => (m.StartTime, m.EndTime));

            return ranges.Concat(maintenanceRanges).ToList();
        }

        /// <summary>
        /// Đề xuất tối đa 3 khung thay thế cùng độ dài với [start, end],
        /// gần nhất với khung yêu cầu, chỉ trong khung hoạt động 07:00–22:00.
        /// </summary>
        public static IReadOnlyList<AvailabilitySlotDto> SuggestAlternatives(
            IReadOnlyList<(DateTime Start, DateTime End)> blockedRanges,
            DateTime start,
            DateTime end)
        {
            var windowStart = start.AddDays(-3).Date + TimeSpan.FromHours(7);
            var windowEnd = end.AddDays(3).Date + TimeSpan.FromHours(22);

            var gaps = Scheduling.FreeGaps(windowStart, windowEnd, blockedRanges, TimeSpan.FromHours(7), TimeSpan.FromHours(22));
            return Scheduling.SuggestSlots(gaps, start, end - start)
                .Select(s => new AvailabilitySlotDto(s.Start, s.End, "Free", null))
                .ToList();
        }
    }
}