using LabBooking.Domain.Entities;

namespace LabBooking.Domain.Interfaces
{
    /// <summary>Cặp token cấp cho người dùng sau khi đăng nhập.</summary>
    public record TokenResult(string AccessToken, string RefreshToken, int ExpiresInSeconds, RefreshToken RefreshTokenEntity);

    /// <summary>
    /// Phát hành cặp access token (JWT) + refresh token. Refresh token entity
    /// do tầng gọi lưu xuống DB qua repository.
    /// </summary>
    public interface ITokenService
    {
        Task<TokenResult> GenerateAsync(User user, CancellationToken cancellationToken = default);
    }
}
