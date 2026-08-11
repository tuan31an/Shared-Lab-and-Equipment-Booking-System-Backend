using LabBooking.Application.Common;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Incidents.Commands
{
    public class CreateIncidentCommand : IRequest<IncidentDto>
    {
        [Required(ErrorMessage = "ResourceId is required.")]
        public Guid ResourceId { get; set; }

        public Guid? BookingId { get; set; }

        [Required(ErrorMessage = "Description is required.")]
        [MaxLength(1000)]
        public string Description { get; set; } = string.Empty;

        [MaxLength(500)]
        public string? ImageUrl { get; set; }
    }

    public class CreateIncidentCommandHandler : IRequestHandler<CreateIncidentCommand, IncidentDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Incident> _incidents;
        private readonly IRepository<Notification> _notifications;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CreateIncidentCommandHandler(
            IRepository<Resource> resources,
            IRepository<Booking> bookings,
            IRepository<Incident> incidents,
            IRepository<Notification> notifications,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _resources = resources;
            _bookings = bookings;
            _incidents = incidents;
            _notifications = notifications;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<IncidentDto> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            var description = request.Description.Trim();
            if (description.Length == 0)
                throw new ArgumentException("Description is required.");

            if (request.BookingId.HasValue)
            {
                var booking = await _bookings.GetByIdAsync(request.BookingId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Booking {request.BookingId} not found.");
                if (booking.ResourceId != request.ResourceId)
                    throw new ArgumentException("BookingId does not belong to the specified resource.");
            }

            var reportedBy = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var incident = new Incident
            {
                ResourceId = request.ResourceId,
                BookingId = request.BookingId,
                ReportedBy = reportedBy,
                Description = description,
                ImageUrl = request.ImageUrl?.Trim(),
                Status = IncidentStatus.Open,
                ReportedAt = DateTime.UtcNow
            };

            await _incidents.AddAsync(incident, cancellationToken);

            // Thông báo cho Lab Manager phụ trách phòng/thiết bị (nếu có).
            if (resource.LabManagerId != null && resource.LabManagerId != reportedBy)
            {
                await _notifications.AddAsync(new Notification
                {
                    UserId = resource.LabManagerId.Value,
                    Type = NotificationType.IncidentReported,
                    Content = $"Incident reported on {resource.Name}: {incident.Description}"
                }, cancellationToken);
            }

            await _uow.SaveChangesAsync(cancellationToken);

            return new IncidentDto(
                incident.Id,
                incident.ResourceId,
                resource.Name,
                incident.BookingId,
                incident.ReportedBy,
                null,
                incident.Description,
                incident.ImageUrl,
                incident.Status.ToString(),
                incident.ReportedAt);
        }
    }
}
