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

        public string? ImageUrl { get; set; }
    }

    public class CreateIncidentCommandHandler : IRequestHandler<CreateIncidentCommand, IncidentDto>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Incident> _incidents;
        private readonly IRepository<Notification> _notifications;
        private readonly ICurrentUser _currentUser;
        private readonly IUnitOfWork _uow;

        public CreateIncidentCommandHandler(
            IRepository<Resource> resources,
            IRepository<Incident> incidents,
            IRepository<Notification> notifications,
            ICurrentUser currentUser,
            IUnitOfWork uow)
        {
            _resources = resources;
            _incidents = incidents;
            _notifications = notifications;
            _currentUser = currentUser;
            _uow = uow;
        }

        public async Task<IncidentDto> Handle(CreateIncidentCommand request, CancellationToken cancellationToken)
        {
            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            var reportedBy = _currentUser.UserId
                ?? throw new UnauthorizedException("Authentication required.");

            var incident = new Incident
            {
                ResourceId = request.ResourceId,
                BookingId = request.BookingId,
                ReportedBy = reportedBy,
                Description = request.Description.Trim(),
                ImageUrl = request.ImageUrl,
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