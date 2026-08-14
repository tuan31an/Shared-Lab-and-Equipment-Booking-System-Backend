using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Auth.Commands
{
    public class RefreshCommand : IRequest<AuthResponse>
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        [MaxLength(128)]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RefreshCommandHandler : IRequestHandler<RefreshCommand, AuthResponse>
    {
        private readonly IRepository<RefreshToken> _refreshTokens;
        private readonly IRepository<User> _users;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;

        public RefreshCommandHandler(IRepository<RefreshToken> refreshTokens, IRepository<User> users, ITokenService tokenService, IUnitOfWork uow)
        {
            _refreshTokens = refreshTokens;
            _users = users;
            _tokenService = tokenService;
            _uow = uow;
        }

        public async Task<AuthResponse> Handle(RefreshCommand request, CancellationToken cancellationToken)
        {
            var refreshToken = await _refreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);
            if (refreshToken == null || refreshToken.RevokedAtUtc != null || refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
                throw new UnauthorizedException("Refresh token is invalid or expired.");

            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            _refreshTokens.Update(refreshToken);
            // Lưu ngay việc thu hồi trước khi kiểm tra user — nếu user bị khoá,
            // token vẫn bị đốt thay vì còn hiệu lực tới khi hết hạn.
            await _uow.SaveChangesAsync(cancellationToken);

            var user = await _users.GetByIdAsync(refreshToken.UserId, cancellationToken);
            if (user == null || user.Status == UserStatus.Disabled)
                throw new UnauthorizedException("Refresh token is invalid or expired.");

            return await AuthResultFactory.BuildAsync(user, _refreshTokens, _tokenService, _uow, cancellationToken);
        }
    }
}
