namespace LabBooking.Application.Contracts
{
    public record IncidentDto(
        Guid Id,
        Guid ResourceId,
        string? ResourceName,
        Guid? BookingId,
        Guid ReportedBy,
        string? ReportedByName,
        string Description,
        string? ImageUrl,
        string Status,
        DateTime ReportedAt);
}