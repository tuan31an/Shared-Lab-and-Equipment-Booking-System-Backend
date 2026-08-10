using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Bookings;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Bookings.Commands
{
    public class RejectBookingCommand : IRequest<BookingDto>
    {
        public Guid BookingId { get; set; }

        public string? Reason { get; set; }
    }

    public class RejectBookingCommandHandler : IRequestHandler<RejectBookingCommand, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public RejectBookingCommandHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<BookingDto> Handle(RejectBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking {request.BookingId} not found.");

            if (booking.Status != BookingStatus.Pending)
                throw new ArgumentException("Only pending bookings can be rejected.");

            var currentUser = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            if (_currentUser.Role != "Admin")
            {
                var resource = await _resources.GetByIdAsync(booking.ResourceId, cancellationToken);
                if (resource?.LabManagerId != currentUser)
                    throw new UnauthorizedException("Only the Lab Manager of this resource can reject bookings.");
            }

            booking.Status = BookingStatus.Rejected;
            booking.ApprovedBy = currentUser;
            booking.ApprovedAt = DateTime.UtcNow;
            booking.MarkUpdated();
            _bookings.Update(booking);
            await _uow.SaveChangesAsync(cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }
    }
}