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
        private readonly ICurrentUser _currentUser;

        public GetBookingsQueryHandler(
            IRepository<Booking> bookings,
            IRepository<Resource> resources,
            IRepository<User> users,
            IRepository<PriorityRule> rules,
            ICurrentUser currentUser)
        {
            _bookings = bookings;
            _resources = resources;
            _users = users;
            _rules = rules;
            _currentUser = currentUser;
        }

        public async Task<PaginationResponse<BookingDto>> Handle(GetBookingsQuery request, CancellationToken cancellationToken)
        {
            var all = await _bookings.ListAsync(null, cancellationToken);

            var scoped = await ScopeToRoleAsync(all, cancellationToken);
            var filtered = scoped
                .Where(b =>
                    (!request.Status.HasValue || b.Status == request.Status) &&
                    (!request.ResourceId.HasValue || b.ResourceId == request.ResourceId) &&
                    (!request.RequesterId.HasValue || b.RequesterId == request.RequesterId) &&
                    (!request.From.HasValue || b.EndTime >= request.From.Value) &&
                    (!request.To.HasValue || b.StartTime <= request.To.Value))
                .OrderByDescending(b => b.CreatedAt)
                .ToList();

            var page = filtered.Skip((request.Page - 1) * request.PageSize).Take(request.PageSize).ToList();

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);
            var users = (await _users.GetAllAsync(cancellationToken)).ToDictionary(u => u.Id);
            var rules = (await _rules.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return new PaginationResponse<BookingDto>(
                BookingEvaluation.ToDtos(page, resources, users, rules),
                filtered.Count,
                request.Page,
                request.PageSize);
        }

        /// <summary>
        /// Giới hạn theo vai trò: Requester chỉ thấy lịch của mình; Lab Manager chỉ thấy
        /// lịch trên phòng/thiết bị mình phụ trách; Admin thấy tất cả.
        /// </summary>
        private async Task<IEnumerable<Booking>> ScopeToRoleAsync(
            IReadOnlyList<Booking> all,
            CancellationToken cancellationToken)
        {
            var role = _currentUser.Role;
            if (role == "Admin")
                return all;

            if (role == "LabManager")
            {
                var managedIds = (await _resources.GetAllAsync(cancellationToken))
                    .Where(r => r.LabManagerId == _currentUser.UserId)
                    .Select(r => r.Id)
                    .ToHashSet();
                return all.Where(b => managedIds.Contains(b.ResourceId));
            }

            // Requester (mặc định)
            return all.Where(b => b.RequesterId == _currentUser.UserId);
        }
    }
}