using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Maintenances.Commands
{
    public class CreateMaintenanceCommand : IRequest<MaintenanceDto>
    {
        [Required(ErrorMessage = "ResourceId is required.")]
        public Guid ResourceId { get; set; }

        [Required(ErrorMessage = "StartTime is required.")]
        public DateTime StartTime { get; set; }

        [Required(ErrorMessage = "EndTime is required.")]
        public DateTime EndTime { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public decimal? Cost { get; set; }
    }

    public class CreateMaintenanceCommandHandler : IRequestHandler<CreateMaintenanceCommand, MaintenanceDto>
    {
        private const decimal MaximumCost = 9_999_999_999.99m;

        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Maintenance> _maintenances;
        private readonly IRepository<Booking> _bookings;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CreateMaintenanceCommandHandler(
            IRepository<Resource> resources,
            IRepository<Maintenance> maintenances,
            IRepository<Booking> bookings,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _resources = resources;
            _maintenances = maintenances;
            _bookings = bookings;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<MaintenanceDto> Handle(CreateMaintenanceCommand request, CancellationToken cancellationToken)
        {
            if (request.EndTime <= request.StartTime)
                throw new ArgumentException("EndTime must be after StartTime.");
            if (request.StartTime <= DateTime.UtcNow)
                throw new ArgumentException("StartTime must be in the future.");
            if (request.Cost is < 0 or > MaximumCost)
                throw new ArgumentException($"Cost must be between 0 and {MaximumCost}.");

            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            var currentUser = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");
            if (_currentUser.Role != "Admin" && resource.LabManagerId != currentUser)
                throw new UnauthorizedException("Only the Lab Manager of this resource or an Admin can schedule maintenance.");

            // Bảo trì khoá khung giờ: chặn nếu trùng lịch đặt (Pending/Approved) hoặc đợt bảo trì đang chạy.
            var overlappingBookings = await _bookings.ListAsync(b =>
                b.ResourceId == request.ResourceId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                b.StartTime < request.EndTime && request.StartTime < b.EndTime,
                cancellationToken);
            var overlappingMaintenance = await _maintenances.ListAsync(m =>
                m.ResourceId == request.ResourceId &&
                m.Status != MaintenanceStatus.Completed &&
                m.StartTime < request.EndTime && request.StartTime < m.EndTime,
                cancellationToken);

            if (overlappingBookings.Count > 0 || overlappingMaintenance.Count > 0)
                throw new ConflictException("Maintenance overlaps an existing booking or maintenance schedule.");

            var maintenance = new Maintenance
            {
                ResourceId = request.ResourceId,
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Description = request.Description?.Trim(),
                Cost = request.Cost,
                Status = MaintenanceStatus.Scheduled,
                CreatedBy = currentUser
            };

            await _maintenances.AddAsync(maintenance, cancellationToken);
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
