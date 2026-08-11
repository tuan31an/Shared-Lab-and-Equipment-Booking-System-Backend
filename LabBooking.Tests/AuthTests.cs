using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Features.Auth.Commands;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using LabBooking.Domain.Interfaces;
using Xunit;

namespace LabBooking.Tests;

public class AuthTests
{
    private readonly FakeRepository<User> _users = new();
    private readonly FakeRepository<RefreshToken> _tokens = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly ITokenService _tokenService = new FakeTokenService();

    private User AddUser(string email = "user@test.com", string? password = "secret123")
    {
        var user = new User
        {
            FullName = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = UserRole.Requester,
            Status = UserStatus.Active
        };
        _users.Items.Add(user);
        return user;
    }

    [Fact]
    public async Task Login_With_Valid_Credentials_Returns_Tokens()
    {
        var user = AddUser();
        var handler = new LoginCommandHandler(_users, _tokens, _tokenService, _uow);

        var response = await handler.Handle(new LoginCommand { Email = user.Email, Password = "secret123" }, CancellationToken.None);

        Assert.Equal("access", response.AccessToken);
        Assert.Equal("refresh", response.RefreshToken);
        Assert.Equal(user.Id, response.User.Id);
        Assert.Single(_tokens.Items);
        Assert.True(_uow.SaveCount > 0);
    }

    [Fact]
    public async Task Login_With_Wrong_Password_Throws()
    {
        AddUser();
        var handler = new LoginCommandHandler(_users, _tokens, _tokenService, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand { Email = "user@test.com", Password = "wrong" }, CancellationToken.None));
        Assert.Empty(_tokens.Items);
    }

    [Fact]
    public async Task Login_With_Unknown_Email_Throws()
    {
        var handler = new LoginCommandHandler(_users, _tokens, _tokenService, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LoginCommand { Email = "nobody@test.com", Password = "x" }, CancellationToken.None));
    }

    [Fact]
    public async Task Refresh_Rotates_Token()
    {
        var user = AddUser();
        _tokens.Items.Add(new RefreshToken { UserId = user.Id, Token = "old-token", ExpiresAtUtc = DateTime.UtcNow.AddDays(1) });
        var handler = new RefreshCommandHandler(_tokens, _users, _tokenService, _uow);

        var response = await handler.Handle(new RefreshCommand { RefreshToken = "old-token" }, CancellationToken.None);

        Assert.Equal("refresh", response.RefreshToken);
        var oldToken = _tokens.Items.Single(t => t.Token == "old-token");
        Assert.NotNull(oldToken.RevokedAtUtc);
        var newToken = _tokens.Items.Single(t => t.Token == "refresh");
        Assert.Null(newToken.RevokedAtUtc);
    }

    [Fact]
    public async Task Refresh_Revoked_Token_Throws()
    {
        var user = AddUser();
        _tokens.Items.Add(new RefreshToken { UserId = user.Id, Token = "old-token", ExpiresAtUtc = DateTime.UtcNow.AddDays(1), RevokedAtUtc = DateTime.UtcNow });
        var handler = new RefreshCommandHandler(_tokens, _users, _tokenService, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new RefreshCommand { RefreshToken = "old-token" }, CancellationToken.None));
    }

    [Fact]
    public async Task Refresh_Expired_Token_Throws()
    {
        AddUser();
        _tokens.Items.Add(new RefreshToken { UserId = Guid.NewGuid(), Token = "old-token", ExpiresAtUtc = DateTime.UtcNow.AddMinutes(-1) });
        var handler = new RefreshCommandHandler(_tokens, _users, _tokenService, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new RefreshCommand { RefreshToken = "old-token" }, CancellationToken.None));
    }

    private sealed class FakeTokenService : ITokenService
    {
        public Task<TokenResult> GenerateAsync(User user, CancellationToken ct = default)
            => Task.FromResult(new TokenResult("access", "refresh", 3600,
                new RefreshToken { UserId = user.Id, Token = "refresh", ExpiresAtUtc = DateTime.UtcNow.AddDays(7) }));
    }
}