using System.ComponentModel.DataAnnotations;
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
    public class CheckBookingConflictCommand : IRequest<BookingConflictResponse>
    {
        [Required(ErrorMessage = "ResourceId is required.")]
        public Guid ResourceId { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime EndTime { get; set; }
    }

    public class CheckBookingConflictCommandHandler : IRequestHandler<CheckBookingConflictCommand, BookingConflictResponse>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<Maintenance> _maintenances;

        public CheckBookingConflictCommandHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            IRepository<Maintenance> maintenances)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _maintenances = maintenances;
        }

        public async Task<BookingConflictResponse> Handle(CheckBookingConflictCommand request, CancellationToken cancellationToken)
        {
            if (request.EndTime <= request.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");

            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            var dueBookings = await _bookings.ListAsync(b =>
                b.ResourceId == request.ResourceId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved),
                cancellationToken);
            var dueMaintenances = await _maintenances.ListAsync(m => m.ResourceId == request.ResourceId, cancellationToken);

            var conflicting = BookingEvaluation.Overlapping(dueBookings, request.StartTime, request.EndTime).ToList();
            var suggested = BookingEvaluation.SuggestAlternatives(
                BookingEvaluation.BlockedRanges(request.StartTime, request.EndTime, dueBookings, dueMaintenances),
                request.StartTime,
                request.EndTime);

            if (conflicting.Count == 0)
                return new BookingConflictResponse(false, Array.Empty<BookingDto>(), suggested);

            var resourcesMap = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var usersMap = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rulesMap = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return new BookingConflictResponse(
                true,
                BookingEvaluation.ToDtos(conflicting, resourcesMap, usersMap, rulesMap),
                suggested);
        }
    }
}