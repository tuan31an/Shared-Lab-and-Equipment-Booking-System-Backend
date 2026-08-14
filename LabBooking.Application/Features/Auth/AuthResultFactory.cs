using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;

namespace LabBooking.Application.Features.Auth
{
    internal static class AuthResultFactory
    {
        public static async Task<AuthResponse> BuildAsync(User user, IRepository<RefreshToken> refreshTokens, ITokenService tokenService, IUnitOfWork uow, CancellationToken cancellationToken)
        {
            var tokens = await tokenService.GenerateAsync(user, cancellationToken);
            await refreshTokens.AddAsync(tokens.RefreshTokenEntity, cancellationToken);
            await uow.SaveChangesAsync(cancellationToken);

            return new AuthResponse(
                tokens.AccessToken,
                tokens.RefreshToken,
                tokens.ExpiresInSeconds,
                new UserDto(user.Id, user.FullName, user.Email, user.Role.ToString(), user.Status.ToString(), user.CreatedAt));
        }
    }
}
