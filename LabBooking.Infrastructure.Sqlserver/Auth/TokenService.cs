using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace LabBooking.Infrastructure.Sqlserver.Auth
{
    /// <summary>
    /// Phát hành access token (JWT) và refresh token. Đọc cấu hình Jwt từ appsettings.
    /// </summary>
    public class TokenService : ITokenService
    {
        private readonly IConfiguration _configuration;

        public TokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public Task<TokenResult> GenerateAsync(User user, CancellationToken cancellationToken = default)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection["Key"] ?? throw new InvalidOperationException("Jwt:Key is not configured.");
            var issuer = jwtSection["Issuer"];
            var audience = jwtSection["Audience"];
            var expiryMinutes = int.TryParse(jwtSection["ExpiryMinutes"], out var expiry) ? expiry : 0;
            var refreshExpiryDays = int.TryParse(jwtSection["RefreshExpiryDays"], out var days) ? days : 7;

            var claims = new List<Claim>
            {
                new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new(ClaimTypes.Name, user.FullName ?? user.Email),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.ToString()),
                new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var credentials = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials);

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshExpiryDays)
            };

            return Task.FromResult(new TokenResult(accessToken, refreshToken.Token, expiryMinutes * 60, refreshToken));
        }
    }
}
