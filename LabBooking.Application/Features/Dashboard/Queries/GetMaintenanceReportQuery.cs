using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Dashboard.Queries
{
    public class GetMaintenanceReportQuery : IRequest<MaintenanceReportDto>
    {
        public DateTime? From { get; set; }

        public DateTime? To { get; set; }

        public Guid? ResourceId { get; set; }
    }

    public class GetMaintenanceReportQueryHandler : IRequestHandler<GetMaintenanceReportQuery, MaintenanceReportDto>
    {
        private readonly IRepository<Maintenance> _maintenances;
        private readonly IRepository<Resource> _resources;
        private readonly ICurrentUser _currentUser;

        public GetMaintenanceReportQueryHandler(
            IRepository<Maintenance> maintenances,
            IRepository<Resource> resources,
            ICurrentUser currentUser)
        {
            _maintenances = maintenances;
            _resources = resources;
            _currentUser = currentUser;
        }

        public async Task<MaintenanceReportDto> Handle(GetMaintenanceReportQuery request, CancellationToken cancellationToken)
        {
            if (_currentUser.Role != "Admin")
                throw new UnauthorizedException("Only an Admin can view the maintenance report.");

            var to = (request.To ?? DateTime.UtcNow).Date.AddDays(1);
            var from = (request.From ?? to.AddDays(-30)).Date;
            if (to <= from)
                throw new ArgumentException("To must be after From.");

            var all = await _maintenances.ListAsync(null, cancellationToken);
            var items = all
                .Where(m =>
                    m.StartTime <= to && m.EndTime >= from &&
                    (!request.ResourceId.HasValue || m.ResourceId == request.ResourceId))
                .OrderBy(m => m.StartTime)
                .ToList();

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            var byResource = items
                .GroupBy(m => m.ResourceId)
                .Select(g =>
                {
                    resources.TryGetValue(g.Key, out var resource);
                    return new MaintenanceCostByResourceDto(
                        g.Key,
                        resource?.Name,
                        g.Count(),
                        g.Sum(m => m.Cost));
                })
                .OrderByDescending(r => r.TotalCost ?? 0)
                .ToList();

            return new MaintenanceReportDto(
                from,
                to,
                items.Count,
                items.Sum(m => m.Cost),
                items.Select(m =>
                {
                    resources.TryGetValue(m.ResourceId, out var resource);
                    return new MaintenanceDto(
                        m.Id,
                        m.ResourceId,
                        resource?.Name,
                        m.StartTime,
                        m.EndTime,
                        m.Description,
                        m.Cost,
                        m.Status.ToString(),
                        m.CreatedBy);
                }).ToList(),
                byResource);
        }
    }
}