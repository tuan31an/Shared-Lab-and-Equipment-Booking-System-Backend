using System.ComponentModel.DataAnnotations;
using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using MediatR;

namespace LabBooking.Application.Features.Auth.Commands
{
    public class LoginCommand : IRequest<AuthResponse>
    {
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Email is not a valid email address.")]
        [MaxLength(200)]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; } = string.Empty;
    }

    public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponse>
    {
        private readonly IRepository<User> _users;
        private readonly IRepository<RefreshToken> _refreshTokens;
        private readonly ITokenService _tokenService;
        private readonly IUnitOfWork _uow;

        public LoginCommandHandler(IRepository<User> users, IRepository<RefreshToken> refreshTokens, ITokenService tokenService, IUnitOfWork uow)
        {
            _users = users;
            _refreshTokens = refreshTokens;
            _tokenService = tokenService;
            _uow = uow;
        }

        public async Task<AuthResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var email = request.Email.Trim();
            var user = await _users.FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
            if (user == null || string.IsNullOrWhiteSpace(user.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                throw new UnauthorizedException("Invalid email or password.");

            if (user.Status == UserStatus.Disabled)
                throw new UnauthorizedException("This account is disabled.");

            return await AuthResultFactory.BuildAsync(user, _refreshTokens, _tokenService, _uow, cancellationToken);
        }
    }
}
