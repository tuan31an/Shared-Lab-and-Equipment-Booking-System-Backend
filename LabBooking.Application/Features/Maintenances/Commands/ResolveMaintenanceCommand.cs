using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Maintenances.Commands
{
    public class ResolveMaintenanceCommand : IRequest<MaintenanceDto>
    {
        public Guid MaintenanceId { get; set; }

        public decimal? Cost { get; set; }
    }

    public class ResolveMaintenanceCommandHandler : IRequestHandler<ResolveMaintenanceCommand, MaintenanceDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Maintenance> _maintenances;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public ResolveMaintenanceCommandHandler(
            IRepository<Resource> resources,
            IRepository<Maintenance> maintenances,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _resources = resources;
            _maintenances = maintenances;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<MaintenanceDto> Handle(ResolveMaintenanceCommand request, CancellationToken cancellationToken)
        {
            var maintenance = await _maintenances.GetByIdAsync(request.MaintenanceId, cancellationToken)
                ?? throw new NotFoundException($"Maintenance {request.MaintenanceId} not found.");

            if (maintenance.Status == MaintenanceStatus.Completed)
                throw new ArgumentException("This maintenance is already completed.");

            var resource = await _resources.GetByIdAsync(maintenance.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {maintenance.ResourceId} not found.");

            var currentUser = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");
            if (_currentUser.Role != "Admin" && resource.LabManagerId != currentUser)
                throw new UnauthorizedException("Only the Lab Manager of this resource or an Admin can resolve maintenance.");

            maintenance.Status = MaintenanceStatus.Completed;
            if (request.Cost.HasValue)
                maintenance.Cost = request.Cost;
            maintenance.MarkUpdated();
            _maintenances.Update(maintenance);
            await _uow.SaveChangesAsync(cancellationToken);

            return new MaintenanceDto(
                maintenance.Id,
                maintenance.ResourceId,
                resource.Name,
                maintenance.StartTime,
                maintenance.EndTime,
                maintenance.Description,
                maintenance.Cost,
                maintenance.Status.ToString(),
                maintenance.CreatedBy);
        }
    }
}