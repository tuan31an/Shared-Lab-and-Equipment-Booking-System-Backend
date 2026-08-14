using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Infrastructure.Sqlserver.Auth;
using Microsoft.IdentityModel.Tokens;
using Xunit;

namespace LabBooking.Tests;

public class TokenServiceTests
{
    private const string Key = "super-secret-key-that-is-long-enough-for-hmac-sha256!!";

    private static User User() => new()
    {
        FullName = "Alice",
        Email = "alice@test.com",
        Role = UserRole.LabManager
    };

    private static TokenService Service() => new(TestConfig.Build(
        ("Jwt:Key", Key),
        ("Jwt:Issuer", "lab-booking"),
        ("Jwt:Audience", "lab-booking-users"),
        ("Jwt:ExpiryMinutes", "30"),
        ("Jwt:RefreshExpiryDays", "7")));

    [Fact]
    public async Task Generate_Issues_Signable_Jwt_With_Expected_Claims()
    {
        var user = User();
        var service = Service();

        var result = await service.GenerateAsync(user);

        var claims = new JwtSecurityTokenHandler().ReadJwtToken(result.AccessToken).Claims.ToDictionary(c => c.Type);
        Assert.Equal(user.Id.ToString(), claims[JwtRegisteredClaimNames.Sub].Value);
        Assert.Equal(user.Email, claims[ClaimTypes.Email].Value);
        Assert.Equal(user.FullName, claims[ClaimTypes.Name].Value);
        Assert.Equal(user.Role.ToString(), claims[ClaimTypes.Role].Value);
        Assert.NotNull(claims[JwtRegisteredClaimNames.Jti].Value);
    }

    [Fact]
    public async Task Generate_Token_Validates_Signature_And_Times()
    {
        var user = User();
        var service = Service();

        var result = await service.GenerateAsync(user);
        var before = DateTime.UtcNow.AddMinutes(-1);

        var handler = new JwtSecurityTokenHandler();
        var parameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "lab-booking",
            ValidateAudience = true,
            ValidAudience = "lab-booking-users",
            ValidateLifetime = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)),
            ClockSkew = TimeSpan.Zero
        };
        var principal = handler.ValidateToken(result.AccessToken, parameters, out _);

        // JwtSecurityTokenHandler maps "sub" → ClaimTypes.NameIdentifier khi validate.
        Assert.Equal(user.Id.ToString(), principal.FindFirst(ClaimTypes.NameIdentifier)?.Value);
        Assert.Equal(user.Role.ToString(), principal.FindFirst(ClaimTypes.Role)?.Value);

        var jwt = handler.ReadJwtToken(result.AccessToken);
        Assert.Equal("lab-booking", jwt.Issuer);
        Assert.Equal("lab-booking-users", jwt.Audiences.First());
        Assert.Equal(user.Id.ToString(), jwt.Claims.First(c => c.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.True(jwt.ValidTo > before, "Token should expire in the future.");
        Assert.True(jwt.ValidTo <= DateTime.UtcNow.AddMinutes(30), "Expiry should honour configured ExpiryMinutes.");
        Assert.Equal(1800, result.ExpiresInSeconds);
    }

    [Fact]
    public async Task Generate_Returns_Random_Refresh_Token_With_Expiry()
    {
        var user = User();
        var service = Service();

        var first = await service.GenerateAsync(user);
        var second = await service.GenerateAsync(user);

        Assert.NotEqual(first.RefreshToken, second.RefreshToken);
        Assert.Equal(64, Convert.FromBase64String(first.RefreshToken).Length);
        Assert.Equal(user.Id, first.RefreshTokenEntity.UserId);
        Assert.InRange(first.RefreshTokenEntity.ExpiresAtUtc, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7));
    }

    [Fact]
    public async Task Generate_Throws_When_Refresh_Expiry_Unset()
    {
        var service = new TokenService(TestConfig.Build(
            ("Jwt:Key", Key),
            ("Jwt:Issuer", "lab-booking"),
            ("Jwt:Audience", "lab-booking-users"),
            ("Jwt:ExpiryMinutes", "30")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(User()));
    }

    [Fact]
    public async Task Generate_Throws_When_Key_Missing()
    {
        var service = new TokenService(TestConfig.Build(("Jwt:Issuer", "x")));

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.GenerateAsync(User()));
    }
}