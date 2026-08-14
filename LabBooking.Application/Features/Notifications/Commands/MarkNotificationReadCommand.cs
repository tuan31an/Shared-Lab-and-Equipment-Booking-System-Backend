using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Notifications.Commands
{
    public class MarkNotificationReadCommand : IRequest<NotificationDto>
    {
        public Guid NotificationId { get; set; }
    }

    public class MarkNotificationReadCommandHandler : IRequestHandler<MarkNotificationReadCommand, NotificationDto>
    {
        private readonly IRepository<Notification> _notifications;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public MarkNotificationReadCommandHandler(
            IRepository<Notification> notifications,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _notifications = notifications;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<NotificationDto> Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
        {
            var notification = await _notifications.GetByIdAsync(request.NotificationId, cancellationToken)
                ?? throw new NotFoundException($"Notification {request.NotificationId} not found.");

            if (notification.UserId != _currentUser.UserId)
                throw new UnauthorizedException("You can only mark your own notifications as read.");

            notification.IsRead = true;
            _notifications.Update(notification);
            await _uow.SaveChangesAsync(cancellationToken);

            return new NotificationDto(
                notification.Id,
                notification.Type.ToString(),
                notification.Content,
                notification.IsRead,
                notification.CreatedAt);
        }
    }
}