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
    public class ApproveBookingCommand : IRequest<BookingDto>
    {
        public Guid BookingId { get; set; }
    }

    public class ApproveBookingCommandHandler : IRequestHandler<ApproveBookingCommand, BookingDto>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<CheckInOut> _checkInOuts;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public ApproveBookingCommandHandler(
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

        public async Task<BookingDto> Handle(ApproveBookingCommand request, CancellationToken cancellationToken)
        {
            var booking = await _bookings.GetByIdAsync(request.BookingId, cancellationToken)
                ?? throw new NotFoundException($"Booking {request.BookingId} not found.");

            if (booking.Status != BookingStatus.Pending)
                throw new ArgumentException("Only pending bookings can be approved.");

            var resource = await _resources.GetByIdAsync(booking.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {booking.ResourceId} not found.");

            var currentUser = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            if (_currentUser.Role != "Admin" && resource.LabManagerId != currentUser)
                throw new UnauthorizedException("Only the Lab Manager of this resource can approve bookings.");

            var approvedOverlaps = (await _bookings.ListAsync(
                b => b.ResourceId == booking.ResourceId && b.Status == BookingStatus.Approved, cancellationToken))
                .Where(b => b.Id != booking.Id)
                .Where(b => LabBooking.Domain.Scheduling.Scheduling.IsOverlap(
                    b.StartTime, b.EndTime, booking.StartTime, booking.EndTime))
                .ToList();

            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            if (approvedOverlaps.Count > 0)
            {
                var requestedPriority = PriorityOf(booking.RuleId, rules);

                if (approvedOverlaps.All(b => PriorityOf(b.RuleId, rules) > requestedPriority))
                {
                    // Lịch được duyệt có ưu tiên thấp hơn → tự động thu hồi để nhường khung giờ.
                    foreach (var loser in approvedOverlaps)
                    {
                        loser.Status = BookingStatus.Rejected;
                        loser.MarkUpdated();
                        _bookings.Update(loser);
                    }
                }
                else
                {
                    throw new ConflictException(
                        "Overlapping approved booking has equal or higher priority. Reject it before approving this one.");
                }
            }

            booking.Status = BookingStatus.Approved;
            booking.ApprovedBy = currentUser;
            booking.ApprovedAt = DateTime.UtcNow;
            booking.MarkUpdated();
            _bookings.Update(booking);
            await _uow.SaveChangesAsync(cancellationToken);

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            await BookingEvaluation.AttachCheckInsAsync(_checkInOuts, new[] { booking }, cancellationToken);
            return BookingEvaluation.ToDto(booking, resources, users, rules);
        }

        /// <summary>Số càng nhỏ càng ưu tiên cao; không có quy tắc → ưu tiên thấp nhất.</summary>
        private static int PriorityOf(Guid? ruleId, IReadOnlyDictionary<Guid, PriorityRule> rules)
            => ruleId.HasValue && rules.TryGetValue(ruleId.Value, out var rule) ? rule.PriorityLevel : int.MaxValue;
    }
}