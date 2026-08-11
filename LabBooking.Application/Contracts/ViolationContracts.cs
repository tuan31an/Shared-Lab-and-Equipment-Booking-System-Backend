namespace LabBooking.Application.Contracts
{
    public record ViolationDto(
        Guid Id,
        Guid UserId,
        string? UserName,
        Guid? BookingId,
        string Type,
        DateTime RecordedAt,
        string? Note);
}