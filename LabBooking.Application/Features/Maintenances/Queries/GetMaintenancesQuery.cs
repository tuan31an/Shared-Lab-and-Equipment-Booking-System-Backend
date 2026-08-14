using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Maintenances.Queries
{
    public class GetMaintenancesQuery : IRequest<IReadOnlyList<MaintenanceDto>>
    {
        public Guid? ResourceId { get; set; }

        public MaintenanceStatus? Status { get; set; }
    }

    public class GetMaintenancesQueryHandler : IRequestHandler<GetMaintenancesQuery, IReadOnlyList<MaintenanceDto>>
    {
        private readonly IRepository<Maintenance> _maintenances;
        private readonly IRepository<Resource> _resources;

        public GetMaintenancesQueryHandler(
            IRepository<Maintenance> maintenances,
            IRepository<Resource> resources)
        {
            _maintenances = maintenances;
            _resources = resources;
        }

        public async Task<IReadOnlyList<MaintenanceDto>> Handle(GetMaintenancesQuery request, CancellationToken cancellationToken)
        {
            var all = await _maintenances.ListAsync(null, cancellationToken);

            var filtered = all
                .Where(m =>
                    (!request.ResourceId.HasValue || m.ResourceId == request.ResourceId) &&
                    (!request.Status.HasValue || m.Status == request.Status))
                .OrderBy(m => m.StartTime)
                .ToList();

            var resources = (await _resources.GetAllAsync(cancellationToken)).ToDictionary(r => r.Id);

            return filtered.Select(m =>
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
            }).ToList();
        }
    }
}