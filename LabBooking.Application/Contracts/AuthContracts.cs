namespace LabBooking.Application.Contracts
{
    public record UserDto(Guid Id, string FullName, string Email, string Role, string Status, DateTime CreatedAt);

    public record AuthResponse(string AccessToken, string RefreshToken, int ExpiresIn, UserDto User);
}
