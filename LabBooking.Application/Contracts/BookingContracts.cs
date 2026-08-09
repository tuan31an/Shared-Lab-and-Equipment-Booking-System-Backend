using System.Text.Json.Serialization;

namespace LabBooking.Application.Contracts
{
    public record BookingDto(
        Guid Id,
        Guid ResourceId,
        string? ResourceName,
        Guid RequesterId,
        string? RequesterName,
        Guid? PriorityRuleId,
        string? PriorityRuleName,
        DateTime StartTime,
        DateTime EndTime,
        string Purpose,
        string Status,
        Guid? ApprovedBy,
        DateTime? ApprovedAt,
        DateTime CreatedAt);

    public record AvailabilitySlotDto(
        DateTime StartTime,
        DateTime EndTime,
        [property: JsonPropertyName("status")] string Status,
        Guid? BookingId);

    public record BookingConflictResponse(
        bool HasConflict,
        IReadOnlyList<BookingDto> ConflictingBookings,
        IReadOnlyList<AvailabilitySlotDto> SuggestedSlots);
}