using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Bookings;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Bookings.Queries
{
    public class GetBookingByIdQuery : IRequest<BookingDto>
    {
        public Guid BookingId { get; set; }
    }

    public class GetBookingByIdQueryHandler : IRequestHandler<GetBookingByIdQuery, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;

        public GetBookingByIdQueryHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
        }

        public async Task<BookingDto> Handle(GetBookingByIdQuery request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking {request.BookingId} not found.");

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }
    }
}