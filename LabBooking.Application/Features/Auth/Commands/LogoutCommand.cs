using System.ComponentModel.DataAnnotations;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Auth.Commands
{
    public class LogoutCommand : IRequest
    {
        [Required(ErrorMessage = "RefreshToken is required.")]
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class LogoutCommandHandler : IRequestHandler<LogoutCommand>
    {
        private readonly IRepository<RefreshToken> _refreshTokens;
        private readonly IUnitOfWork _uow;

        public LogoutCommandHandler(IRepository<RefreshToken> refreshTokens, IUnitOfWork uow)
        {
            _refreshTokens = refreshTokens;
            _uow = uow;
        }

        public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
        {
            var refreshToken = await _refreshTokens.FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken, cancellationToken);
            if (refreshToken != null && refreshToken.RevokedAtUtc == null)
            {
                refreshToken.RevokedAtUtc = DateTime.UtcNow;
                _refreshTokens.Update(refreshToken);
                await _uow.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
