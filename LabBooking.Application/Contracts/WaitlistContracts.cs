namespace LabBooking.Application.Contracts
{
    public record WaitlistDto(
        Guid Id,
        Guid ResourceId,
        string? ResourceName,
        Guid RequesterId,
        DateTime DesiredStart,
        DateTime DesiredEnd,
        string Status,
        DateTime? NotifiedAt,
        DateTime CreatedAt);
}