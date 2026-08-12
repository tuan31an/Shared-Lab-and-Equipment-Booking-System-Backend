using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Users.Commands
{
    public class DeleteUserCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    public class DeleteUserCommandHandler : IRequestHandler<DeleteUserCommand>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<Booking> _bookings;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public DeleteUserCommandHandler(IRepository<User> users, IRepository<Booking> bookings, ICurrentUser currentUser, IUnitOfWork uow)
        {
            _users = users;
            _bookings = bookings;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task Handle(DeleteUserCommand request, CancellationToken cancellationToken)
        {
            var user = await _users.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"User {request.Id} not found.");

            if (user.Id == _currentUser.UserId)
                throw new ConflictException("You cannot delete your own account.");

            var now = DateTime.UtcNow;
            var activeBooking = await _bookings.FirstOrDefaultAsync(b =>
                b.RequesterId == request.Id &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                b.EndTime > now,
                cancellationToken);
            if (activeBooking != null)
                throw new ConflictException("User has an active or upcoming booking and cannot be deleted.");

            user.IsDeleted = true;
            user.MarkUpdated();
            _users.Update(user);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
