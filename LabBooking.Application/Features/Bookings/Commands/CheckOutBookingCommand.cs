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
    public class CheckOutBookingCommand : IRequest<BookingDto>
    {
        public Guid BookingId { get; set; }
    }

    public class CheckOutBookingCommandHandler : IRequestHandler<CheckOutBookingCommand, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<CheckInOut> _checkInOuts;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CheckOutBookingCommandHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            IRepository<CheckInOut> checkInOuts,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _checkInOuts = checkInOuts;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CheckOutBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking {request.BookingId} not found.");

            var currentUser = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var resource = await _resources.GetByIdAsync(booking.ResourceId, cancellationToken);
            var isManagerOrAdmin = _currentUser.Role == "Admin" || resource?.LabManagerId == currentUser;
            if (booking.RequesterId != currentUser && !isManagerOrAdmin)
                throw new UnauthorizedException("Only the requester, the Lab Manager, or an Admin can check out.");

            var record = (await _checkInOuts.ListAsync(c => c.BookingId == booking.Id, cancellationToken)).FirstOrDefault()
                ?? throw new ArgumentException("Booking has not been checked in yet.");

            if (record.CheckInTime == null)
                throw new ArgumentException("Booking has not been checked in yet.");

            if (record.CheckOutTime != null)
                throw new ArgumentException("This booking has already been checked out.");

            record.CheckOutTime = DateTime.UtcNow;
            record.ActualDuration = (int)record.CheckOutTime.Value.Subtract(record.CheckInTime.Value).TotalMinutes;
            _checkInOuts.Update(record);
            await _uow.SaveChangesAsync(cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            booking.CheckInOut = record;

            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }
    }
}