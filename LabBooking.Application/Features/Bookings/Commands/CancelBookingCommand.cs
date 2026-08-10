using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Bookings;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Configuration;

namespace LabBooking.Application.Features.Bookings.Commands
{
    public class CancelBookingCommand : IRequest<BookingDto>
    {
        public Guid BookingId { get; set; }
    }

    public class CancelBookingCommandHandler : IRequestHandler<CancelBookingCommand, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<CheckInOut> _checkInOuts;
        private readonly ICurrentUser _currentUser;
        private readonly IConfiguration _configuration;
        private readonly IUnitOfWork _uow;

        public CancelBookingCommandHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            IRepository<CheckInOut> checkInOuts,
            ICurrentUser currentUser,
            IConfiguration configuration,
            IUnitOfWork uow)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _checkInOuts = checkInOuts;
            _currentUser = currentUser;
            _configuration = configuration;
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CancelBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking {request.BookingId} not found.");

            if (booking.RequesterId != _currentUser.UserId)
                throw new UnauthorizedException("You can only cancel your own bookings.");

            if (booking.Status is BookingStatus.Completed or BookingStatus.Cancelled or BookingStatus.Rejected)
                throw new ArgumentException("This booking cannot be cancelled in its current state.");

            var deadlineHours = double.TryParse(_configuration["Booking:CancellationDeadlineHours"], out var hours) ? hours : 2;
            if (!double.IsFinite(deadlineHours) || deadlineHours < 0)
                throw new InvalidOperationException("Booking:CancellationDeadlineHours must be zero or greater.");

            if (DateTime.UtcNow.AddHours(deadlineHours) >= booking.StartTime)
                throw new ArgumentException($"Booking can only be cancelled more than {deadlineHours} hour(s) before the start time.");

            booking.Status = BookingStatus.Cancelled;
            booking.MarkUpdated();
            _bookings.Update(booking);
            await _uow.SaveChangesAsync(cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            await BookingEvaluation.AttachCheckInsAsync(_checkInOuts, new[] { booking }, cancellationToken);

            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }
    }
}
