using LabBooking.Application.Common.Exceptions;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Resources.Commands
{
    public class DeleteResourceCommand : IRequest
    {
        public Guid Id { get; set; }
    }

    public class DeleteResourceCommandHandler : IRequestHandler<DeleteResourceCommand>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Maintenance> _maintenances;
        private readonly IUnitOfWork _uow;

        public DeleteResourceCommandHandler(
            IRepository<Resource> resources,
            IRepository<Booking> bookings,
            IRepository<Maintenance> maintenances,
            IUnitOfWork uow)
        {
            _resources = resources;
            _bookings = bookings;
            _maintenances = maintenances;
            _uow = uow;
        }

        public async Task Handle(DeleteResourceCommand request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.Id, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.Id} not found.");

            var now = DateTime.UtcNow;
            var activeBooking = await _bookings.FirstOrDefaultAsync(b =>
                b.ResourceId == request.Id &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                b.EndTime > now,
                cancellationToken);
            if (activeBooking != null)
                throw new ConflictException("Resource has an active or upcoming booking and cannot be deleted.");

            var activeMaintenance = await _maintenances.FirstOrDefaultAsync(m =>
                m.ResourceId == request.Id &&
                m.Status != MaintenanceStatus.Completed &&
                m.EndTime > now,
                cancellationToken);
            if (activeMaintenance != null)
                throw new ConflictException("Resource has active or upcoming maintenance and cannot be deleted.");

            resource.IsDeleted = true;
            resource.MarkUpdated();
            _resources.Update(resource);
            await _uow.SaveChangesAsync(cancellationToken);
        }
    }
}
