using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using LabBooking.Domain.Scheduling;
using MediatR;
using System.ComponentModel.DataAnnotations;

namespace LabBooking.Application.Features.Resources.Queries
{
    public class GetResourceAvailabilityQuery : IRequest<IReadOnlyList<AvailabilitySlotDto>>
    {
        public Guid ResourceId { get; set; }

        [Required(ErrorMessage = "From is required.")]
        public DateTime From { get; set; }

        [Required(ErrorMessage = "To is required.")]
        public DateTime To { get; set; }
    }

    public class GetResourceAvailabilityQueryHandler : IRequestHandler<GetResourceAvailabilityQuery, IReadOnlyList<AvailabilitySlotDto>>
    {
        private readonly IRepository<Resource> _resources;
        private readonly IRepository<Booking> _bookings;
        private readonly IRepository<Maintenance> _maintenances;

        public GetResourceAvailabilityQueryHandler(
            IRepository<Resource> resources,
            IRepository<Booking> bookings,
            IRepository<Maintenance> maintenances)
        {
            _resources = resources;
            _bookings = bookings;
            _maintenances = maintenances;
        }

        public async Task<IReadOnlyList<AvailabilitySlotDto>> Handle(
            GetResourceAvailabilityQuery request,
            CancellationToken cancellationToken)
        {
            if (request.To <= request.From)
                throw new ArgumentException("To must be after From.");

            var resource = await _resources.GetByIdAsync(request.ResourceId, cancellationToken)
                ?? throw new NotFoundException($"Resource {request.ResourceId} not found.");

            if (resource.Status != ResourceStatus.Available)
            {
                return
                [
                    new AvailabilitySlotDto(request.From, request.To, resource.Status.ToString(), null)
                ];
            }

            var booked = await _bookings.ListAsync(b =>
                b.ResourceId == request.ResourceId &&
                (b.Status == BookingStatus.Pending || b.Status == BookingStatus.Approved) &&
                b.StartTime < request.To && request.From < b.EndTime,
                cancellationToken);

            var maintenance = await _maintenances.ListAsync(m =>
                m.ResourceId == request.ResourceId &&
                m.Status != MaintenanceStatus.Completed &&
                m.StartTime < request.To && request.From < m.EndTime,
                cancellationToken);

            var bookedSlots = booked.Select(b => new AvailabilitySlotDto(
                b.StartTime < request.From ? request.From : b.StartTime,
                b.EndTime > request.To ? request.To : b.EndTime,
                "Booked",
                b.Id));
            var maintenanceSlots = maintenance.Select(m => new AvailabilitySlotDto(
                m.StartTime < request.From ? request.From : m.StartTime,
                m.EndTime > request.To ? request.To : m.EndTime,
                "UnderMaintenance",
                null));

            var busyRanges = booked.Select(b => (b.StartTime, b.EndTime))
                .Concat(maintenance.Select(m => (m.StartTime, m.EndTime)))
                .ToList();

            // Khung hoạt động 07:00–22:00 giờ VN = 00:00–15:00 UTC (UTC+7, không DST).
            var freeSlots = Scheduling.FreeGaps(request.From, request.To, busyRanges, TimeSpan.Zero, TimeSpan.FromHours(15))
                .Select(g => new AvailabilitySlotDto(g.Start, g.End, "Free", null));

            return bookedSlots
                .Concat(maintenanceSlots)
                .Concat(freeSlots)
                .OrderBy(s => s.StartTime)
                .ToList();
        }
    }
}
