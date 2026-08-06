using LabBooking.API.Models;
using LabBooking.Infrastructure.Sqlserver.Persistence;
using LabBooking.Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;
using System.Collections.Generic;
using LabBooking.Domain.Enums;
using System.Net;
using System.Security.Cryptography;

namespace LabBooking.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly ApplicationDbContext _dbContext;

        public AuthController(IConfiguration configuration, ApplicationDbContext dbContext)
        {
            _configuration = configuration;
            _dbContext = dbContext;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.FullName) || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest();

            var email = request.Email.Trim();
            var userExists = await _dbContext.Users.AnyAsync(u => u.Email == email);
            if (userExists)
                return Conflict(ApiResponse.Fail(HttpStatusCode.Conflict, "Email already exists."));

            var user = new User
            {
                FullName = request.FullName.Trim(),
                Email = email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                Role = UserRole.Requester,
                DepartmentId = request.DepartmentId,
                Status = UserStatus.Active
            };

            _dbContext.Users.Add(user);
            await _dbContext.SaveChangesAsync();

            var response = new
            {
                id = user.Id,
                fullName = user.FullName,
                email = user.Email,
                role = user.Role.ToString(),
                status = user.Status.ToString(),
                createdAt = user.CreatedAt
            };

            return Created(string.Empty, response);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return BadRequest();

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
            if (user == null)
                return Unauthorized();

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized();

            var tokenResponse = await GenerateTokenPairAsync(user);
            return Ok(tokenResponse);
        }

        /// <summary>Làm mới access token bằng refresh token. Token cũ bị thu hồi (rotate).</summary>
        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest();

            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken == null ||
                refreshToken.RevokedAtUtc != null ||
                refreshToken.ExpiresAtUtc <= DateTime.UtcNow)
                return Unauthorized(ApiResponse.Fail(HttpStatusCode.Unauthorized, "Refresh token is invalid or expired."));

            // Rotate: thu hồi token cũ, cấp cặp mới.
            refreshToken.RevokedAtUtc = DateTime.UtcNow;
            _dbContext.RefreshTokens.Update(refreshToken);

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == refreshToken.UserId);
            if (user == null)
                return Unauthorized(ApiResponse.Fail(HttpStatusCode.Unauthorized, "Refresh token is invalid or expired."));

            var tokenResponse = await GenerateTokenPairAsync(user);
            return Ok(tokenResponse);
        }

        /// <summary>Đăng xuất, thu hồi refresh token hiện tại.</summary>
        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] RefreshRequest? request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.RefreshToken))
                return BadRequest(ApiResponse.Fail(HttpStatusCode.BadRequest, "RefreshToken is required."));

            var refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.Token == request.RefreshToken);

            if (refreshToken != null && refreshToken.RevokedAtUtc == null)
            {
                refreshToken.RevokedAtUtc = DateTime.UtcNow;
                _dbContext.RefreshTokens.Update(refreshToken);
                await _dbContext.SaveChangesAsync();
            }

            return NoContent();
        }

        private async Task<object> GenerateTokenPairAsync(User user)
        {
            var jwtSection = _configuration.GetSection("Jwt");
            var key = jwtSection.GetValue<string>("Key");
            var issuer = jwtSection.GetValue<string>("Issuer");
            var audience = jwtSection.GetValue<string>("Audience");
            var expiryMinutes = jwtSection.GetValue<int>("ExpiryMinutes");
            var refreshExpiryDays = jwtSection.GetValue<int?>("RefreshExpiryDays") ?? 7;

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.FullName ?? user.Email),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
            var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            var accessToken = new JwtSecurityTokenHandler().WriteToken(token);

            var refreshToken = new RefreshToken
            {
                UserId = user.Id,
                Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
                ExpiresAtUtc = DateTime.UtcNow.AddDays(refreshExpiryDays)
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync();

            return new
            {
                accessToken,
                refreshToken = refreshToken.Token,
                expiresIn = expiryMinutes * 60,
                user = new
                {
                    id = user.Id,
                    fullName = user.FullName,
                    email = user.Email,
                    role = user.Role.ToString()
                }
            };
        }
    }
}