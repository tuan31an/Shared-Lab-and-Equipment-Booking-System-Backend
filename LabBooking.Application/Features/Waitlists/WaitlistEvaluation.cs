using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;

namespace LabBooking.Application.Features.Waitlists
{
    /// <summary>
    /// Hỗ trợ dùng chung cho Waitlist: làm hết hạn các yêu cầu chờ đã qua,
    /// và thông báo theo thứ tự đăng ký trước khi một khung giờ được trống lại
    /// (Booking bị huỷ/từ chối). Dùng chung cho Cancel/Reject và Join nhắc lịch.
    /// </summary>
    internal static class WaitlistEvaluation
    {
        public static WaitlistDto ToDto(Waitlist w, string? resourceName)
            => new(
                w.Id,
                w.ResourceId,
                resourceName,
                w.RequesterId,
                w.DesiredStart,
                w.DesiredEnd,
                w.Status.ToString(),
                w.NotifiedAt,
                w.CreatedAt);
        /// <summary>
        /// Đánh dấu Expired các yêu cầu chờ đã qua DesiredEnd, rồi thông báo
        /// những yêu cầu Waiting còn giao khung giờ với [freedStart, freedEnd].
        /// </summary>
        public static async Task NotifyAvailableAsync(
            IRepository<Waitlist> waitlists,
            IRepository<Notification> notifications,
            Guid resourceId,
            DateTime freedStart,
            DateTime freedEnd,
            CancellationToken cancellationToken)
        {
            var now = DateTime.UtcNow;
            var entries = await waitlists.ListAsync(
                w => w.ResourceId == resourceId && (w.Status == WaitlistStatus.Waiting || w.Status == WaitlistStatus.Notified),
                cancellationToken);

            var expired = entries.Where(w => w.DesiredEnd <= now).ToList();
            foreach (var w in expired)
            {
                w.Status = WaitlistStatus.Expired;
                waitlists.Update(w);
            }

            var eligible = entries
                .Where(w => w.Status == WaitlistStatus.Waiting)
                .Where(w => w.DesiredEnd > now)
                .Where(w => w.DesiredStart < freedEnd && freedStart < w.DesiredEnd)
                .OrderBy(w => w.CreatedAt)
                .FirstOrDefault();

            // Chỉ thông báo người đầu hàng đợi (FIFO) cho 1 khung giờ được trống;
            // các entry sau giữ nguyên Waiting để còn nhận thông báo cho lần trống khác.
            if (eligible == null)
                return;

            eligible.Status = WaitlistStatus.Notified;
            eligible.NotifiedAt = now;
            waitlists.Update(eligible);
            await notifications.AddAsync(new Notification
            {
                UserId = eligible.RequesterId,
                Type = NotificationType.WaitlistAvailable,
                Content = $"A slot you were waiting for on resource {resourceId} is now available."
            }, cancellationToken);
        }
    }
}