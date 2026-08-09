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
    public class CreateBookingCommand : IRequest<BookingDto>
    {
        [Required(ErrorMessage = "ResourceId is required.")]
        public Guid ResourceId { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Purpose is required.")]
        [MaxLength(500)]
        public string Purpose { get; set; } = string.Empty;

        public Guid? PriorityRuleId { get; set; }
    }

    public class CreateBookingCommandHandler : IRequestHandler<CreateBookingCommand, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<Maintenance> _maintenances;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CreateBookingCommandHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            IRepository<Maintenance> maintenances,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _maintenances = maintenances;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<BookingDto> Handle(CreateBookingCommand request, CancellationToken cancellationToken)
        {
            if (request.EndTime <= request.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");

            if (request.StartTime <= DateTime.UtcNow)
                throw new ArgumentException("StartTime must be in the future.");

            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            if (request.PriorityRuleId.HasValue && await _rules.GetByIdAsync(request.PriorityRuleId.Value, cancellationToken) == null)
                throw new NotFoundException($"Priority rule {request.PriorityRuleId} not found.");

            var requesterId = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var dueBookings = await _bookings.ListAsync(b =>
                b.ResourceId == request.ResourceId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved),
                cancellationToken);
            var dueMaintenances = await _maintenances.ListAsync(m => m.ResourceId == request.ResourceId, cancellationToken);

            var conflicts = BookingEvaluation.Overlapping(dueBookings, request.StartTime, request.EndTime).ToList();
            var maintenanceOverlap = dueMaintenances
                .Where(m => m.StartTime < request.EndTime && request.StartTime < m.EndTime)
                .ToList();

            if (conflicts.Count > 0 || maintenanceOverlap.Count > 0)
            {
                var resourcesMap = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
                var usersMap = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
                var rulesMap = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

                var response = new BookingConflictResponse(
                    HasConflict: true,
                    ConflictingBookings: BookingEvaluation.ToDtos(conflicts, resourcesMap, usersMap, rulesMap),
                    SuggestedSlots: BookingEvaluation.SuggestAlternatives(
                        BookingEvaluation.BlockedRanges(request.StartTime, request.EndTime, dueBookings, dueMaintenances),
                        request.StartTime,
                        request.EndTime));

                throw new ConflictException("Requested time conflicts with an existing booking or maintenance.", response);
            }

            var booking = new Booking
            {
                ResourceId = request.ResourceId,
                RequesterId = requesterId,
                RuleId = request.PriorityRuleId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Purpose = request.Purpose.Trim(),
                Status = BookingStatus.Pending
            };

            await _bookings.AddAsync(booking, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }
    }
}