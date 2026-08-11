using LabBooking.Application.Common;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Dashboard.Queries
{
    public class GetUsageDashboardQuery : IRequest<UsageDashboardDto>
    {
        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public Guid? ResourceId { get; set; }

        public Guid? DepartmentId { get; set; }
    }

    public class GetUsageDashboardQueryHandler : IRequestHandler<GetUsageDashboardQuery, UsageDashboardDto>
    {
        // Khung hoạt động 07:00–22:00 (15h/ngày), khớp với gợi ý khung thay thế.
        private const double WorkingHoursPerDay = 15;

        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<CheckInOut> _checkInOuts;
        private readonly IRepository<Department> _departments;
        private readonly ICurrentUser _currentUser;

        public GetUsageDashboardQueryHandler(
            IRepository<Resource> resources,
            IRepository<Booking> bookings,
            IRepository<CheckInOut> checkInOuts,
            IRepository<Department> departments,
            ICurrentUser currentUser)
        {
            _resources = resources;
            _bookings = bookings;
            _checkInOuts = checkInOuts;
            _departments = departments;
            _currentUser = currentUser;
        }

        public async Task<UsageDashboardDto> Handle(GetUsageDashboardQuery request, CancellationToken cancellationToken)
        {
            var to = (request.To ?? DateTime.UtcNow).Date.AddDays(1);
            var from = (request.From ?? to.AddDays(-30)).Date;
            if (to <= from)
                throw new ArgumentException("To must be after From.");

            var allResources = await _resources.GetAllAsync(cancellationToken);
            var scopedResources = ScopeResources(allResources);
            var resources = scopedResources
                .Where(r =>
                    (!request.ResourceId.HasValue || r.Id == request.ResourceId) &&
                    (!request.DepartmentId.HasValue || r.DepartmentId == request.DepartmentId))
                .ToList();

            var allBookings = await _bookings.ListAsync(null, cancellationToken);
            var checkIns = (await _checkInOuts.ListAsync(null, cancellationToken)).ToDictionary(c => c.BookingId);
            var departments = (await _departments.GetAllAsync(cancellationToken)).ToDictionary(d => d.Id);

            var capacityDays = Math.Max(1, (to.Date - from.Date).Days);
            var perResource = new List<ResourceUsageDto>();
            var totalBooked = 0;
            var totalActual = 0;
            var totalCapacity = 0;

            foreach (var resource in resources)
            {
                var inWindow = allBookings
                    .Where(b => b.ResourceId == resource.Id)
                    .Where(b => b.Status is BookingStatus.Approved or BookingStatus.Completed)
                    .Where(b => b.EndTime >= from && b.StartTime <= to)
                    .ToList();

                var bookedMinutes = inWindow.Sum(b => OverlapMinutes(b.StartTime, b.EndTime, from, to));
                var actualMinutes = inWindow.Sum(b =>
                    checkIns.TryGetValue(b.Id, out var cio) ? cio.ActualDuration ?? 0 : 0);
                var capacityMinutes = (int)(capacityDays * WorkingHoursPerDay * 60);

                departments.TryGetValue(resource.DepartmentId ?? Guid.Empty, out var dept);
                perResource.Add(new ResourceUsageDto(
                    resource.Id,
                    resource.Name,
                    dept?.Name,
                    bookedMinutes,
                    actualMinutes,
                    Percent(actualMinutes, capacityMinutes)));

                totalBooked += bookedMinutes;
                totalActual += actualMinutes;
                totalCapacity += capacityMinutes;
            }

            var byDepartment = resources
                .GroupBy(r => r.DepartmentId ?? Guid.Empty)
                .Select(g =>
                {
                    departments.TryGetValue(g.Key, out var dept);
                    var resourceIds = g.Select(r => r.Id).ToHashSet();
                    var resStats = perResource.Where(p => resourceIds.Contains(p.ResourceId)).ToList();
                    var deptCapacity = (int)(resStats.Count * capacityDays * WorkingHoursPerDay * 60);
                    return new DepartmentUsageDto(
                        g.Key,
                        dept?.Name ?? "Unassigned",
                        resStats.Sum(x => x.BookedMinutes),
                        resStats.Sum(x => x.ActualMinutes),
                        Percent(resStats.Sum(x => x.ActualMinutes), deptCapacity));
                })
                .OrderByDescending(d => d.ActualMinutes)
                .ToList();

            return new UsageDashboardDto(
                from,
                to,
                Percent(totalActual, totalCapacity),
                totalBooked,
                totalActual,
                perResource.OrderByDescending(r => r.UsagePercent).ToList(),
                byDepartment);
        }

        private IReadOnlyList<Resource> ScopeResources(IReadOnlyList<Resource> all)
        {
            var role = _currentUser.Role;
            if (role == "Admin")
                return all;
            if (role == "LabManager")
                return all.Where(r => r.LabManagerId == _currentUser.UserId).ToList();
            return Array.Empty<Resource>();
        }

        private static int OverlapMinutes(DateTime start, DateTime end, DateTime from, DateTime to)
        {
            var overlapStart = start > from ? start : from;
            var overlapEnd = end < to ? end : to;
            var minutes = overlapEnd.Subtract(overlapStart).TotalMinutes;
            return minutes < 0 ? 0 : (int)minutes;
        }

        private static decimal Percent(int actual, int capacity)
            => capacity <= 0 ? 0 : Math.Round(actual * 100m / capacity, 1);
    }
}