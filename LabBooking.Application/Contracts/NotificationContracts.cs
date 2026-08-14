namespace LabBooking.Application.Contracts
{
    public record NotificationDto(
        Guid Id,
        string Type,
        string Content,
        bool IsRead,
        DateTime CreatedAt);
}