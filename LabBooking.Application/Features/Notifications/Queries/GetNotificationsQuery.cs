using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Notifications.Queries
{
    public class GetNotificationsQuery : IRequest<IReadOnlyList<NotificationDto>>
    {
        public bool UnreadOnly { get; set; }
    }

    public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
    {
        private readonly IRepository<Notification> _notifications;
        private readonly ICurrentUser _currentUser;

        public GetNotificationsQueryHandler(
            IRepository<Notification> notifications,
            ICurrentUser currentUser)
        {
            _notifications = notifications;
            _currentUser = currentUser;
        }

        public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
        {
            var userId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var all = await _notifications.ListAsync(n => n.UserId == userId, cancellationToken);

            return all
                .Where(n => !request.UnreadOnly || !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new NotificationDto(n.Id, n.Type.ToString(), n.Content, n.IsRead, n.CreatedAt))
                .ToList();
        }
    }
}