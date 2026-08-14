using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Bookings;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Bookings.Queries
{
    public class GetBookingsQuery : IRequest<PaginationResponse<BookingDto>>
    {
        [Range(1, int.MaxValue)]
        public int Page { get; set; } = 1;

        [Range(1, 100)]
        public int PageSize { get; set; } = 20;

        public BookingStatus? Status { get; set; }

        public Guid? ResourceId { get; set; }

        public Guid? RequesterId { get; set; }

        public DateTime? From { get; set; }

        public DateTime? To { get; set; }
    }

    public class GetBookingsQueryHandler : IRequestHandler<GetBookingsQuery, PaginationResponse<BookingDto>>
    {
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<User> _users;
        private readonly IRepository<PriorityRule> _rules;
        private readonly IRepository<CheckInOut> _checkInOuts;
        private readonly ICurrentUser _currentUser;

        public GetBookingsQueryHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            IRepository<CheckInOut> checkInOuts,
            ICurrentUser currentUser)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _checkInOuts = checkInOuts;
            _currentUser = currentUser;
        }

        public async Task<PaginationResponse<BookingDto>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
        {
            if (request.From.HasValue && request.To.HasValue && request.To <= request.From)
                throw new ArgumentException("To must be after From.");

            var role = _currentUser.Role;
            var userId = _currentUser.UserId;
            var ownOnly = role != "Admin" && role != "LabManager";

            // Bộ lọc + phạm vi vai trò đẩy xuống EF; LabManager lọc theo resource phụ trách
            // trong RAM (trên tập đã thu hẹp bởi bộ lọc SQL).
            HashSet<Guid>? managedResourceIds = null;
            if (role == "LabManager" && userId.HasValue)
            {
                managedResourceIds = (await _resources.GetAllAsync(cancellationToken))
                    .Where(r => r.LabManagerId == userId)
                    .Select(r => r.Id)
                    .ToHashSet();
            }

            var all = await _bookings.ListAsync(b =>
                (!request.Status.HasValue || b.Status == request.Status) &&
                (!request.ResourceId.HasValue || b.ResourceId == request.ResourceId) &&
                (!request.RequesterId.HasValue || b.RequesterId == request.RequesterId) &&
                (!request.From.HasValue || b.EndTime > request.From.Value) &&
                (!request.To.HasValue || b.StartTime < request.To.Value) &&
                (!ownOnly || b.RequesterId == userId),
                cancellationToken);

            var scoped = role == "LabManager"
                ? all.Where(b => managedResourceIds!.Contains(b.ResourceId)).ToList()
                : all.ToList();

            var filtered = scoped
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            var page = filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            await BookingEvaluation.AttachCheckInsAsync(_checkInOuts, page, cancellationToken);

            return new PaginationResponse<BookingDto>(
                BookingEvaluation.ToDtos(page, resources, users, rules),
                filtered.Count,
                request.Page,
                request.PageSize);
        }
    }
}
