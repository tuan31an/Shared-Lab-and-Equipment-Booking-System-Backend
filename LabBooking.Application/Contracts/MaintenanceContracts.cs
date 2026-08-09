namespace LabBooking.Application.Contracts
{
    public record MaintenanceDto(
        Guid Id,
        Guid ResourceId,
        string? ResourceName,
        DateTime StartTime,
        DateTime EndTime,
        string? Description,
        decimal? Cost,
        string Status,
        Guid? CreatedBy);
}