namespace LabBooking.Application.Contracts
{
    public record RestrictionDto(
        Guid Id,
        Guid UserId,
        string? UserName,
        DateTime StartDate,
        DateTime EndDate,
        string Reason,
        Guid? CreatedBy);
}