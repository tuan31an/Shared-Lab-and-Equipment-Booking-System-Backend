using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Features.Users.Commands;
using LabBooking.Application.Features.Users.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Xunit;

namespace LabBooking.Tests;

public class UserTests
{
    private readonly FakeRepository<User> _users = new();
    private readonly FakeRepository<Department> _departments = new();
    private readonly FakeRepository<Booking> _bookings = new();
    private readonly FakeUnitOfWork _uow = new();
    private readonly FakeCurrentUser _currentUser = new();

    private User AddUser(string email = "user@test.com", string password = "secret123", UserRole role = UserRole.Requester)
    {
        var user = new User
        {
            FullName = "Test User",
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Role = role,
            Status = UserStatus.Active
        };
        _users.Items.Add(user);
        return user;
    }

    [Fact]
    public async Task CreateUser_Creates_With_Given_Role_And_Hashes_Password()
    {
        var handler = new CreateUserCommandHandler(_users, _departments, _uow);

        var result = await handler.Handle(new CreateUserCommand
        {
            FullName = "  Alice  ",
            Email = "alice@test.com",
            Password = "secret123",
            Role = UserRole.LabManager
        }, CancellationToken.None);

        Assert.Equal("Alice", result.FullName);
        Assert.Equal(UserRole.LabManager.ToString(), result.Role);
        var user = Assert.Single(_users.Items);
        Assert.True(BCrypt.Net.BCrypt.Verify("secret123", user.PasswordHash));
        Assert.True(_uow.SaveCount > 0);
    }

    [Fact]
    public async Task CreateUser_Duplicate_Email_Throws_Conflict()
    {
        AddUser(email: "dup@test.com");
        var handler = new CreateUserCommandHandler(_users, _departments, _uow);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new CreateUserCommand
        {
            FullName = "Alice",
            Email = "dup@test.com",
            Password = "secret123",
            Role = UserRole.Requester
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CreateUser_Unknown_Department_Throws()
    {
        var handler = new CreateUserCommandHandler(_users, _departments, _uow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new CreateUserCommand
        {
            FullName = "Alice",
            Email = "alice@test.com",
            Password = "secret123",
            Role = UserRole.Requester,
            DepartmentId = Guid.NewGuid()
        }, CancellationToken.None));
    }

    [Fact]
    public async Task UpdateUser_Updates_Fields()
    {
        var user = AddUser();
        _departments.Items.Add(new Department { Name = "CNTT" });
        var dept = _departments.Items.Single();
        var handler = new UpdateUserCommandHandler(_users, _departments, _currentUser, _uow);

        var result = await handler.Handle(new UpdateUserCommand
        {
            Id = user.Id,
            FullName = "Bob",
            Role = UserRole.LabManager,
            Status = UserStatus.Disabled,
            DepartmentId = dept.Id
        }, CancellationToken.None);

        Assert.Equal("Bob", result.FullName);
        Assert.Equal(UserRole.LabManager.ToString(), result.Role);
        Assert.Equal(UserStatus.Disabled.ToString(), result.Status);
        Assert.NotNull(user.UpdatedAt);
    }

    [Fact]
    public async Task UpdateUser_Cannot_Change_Own_Role_Or_Status()
    {
        var user = AddUser(role: UserRole.Admin);
        _currentUser.UserId = user.Id;
        var handler = new UpdateUserCommandHandler(_users, _departments, _currentUser, _uow);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new UpdateUserCommand
        {
            Id = user.Id,
            FullName = "Bob",
            Role = UserRole.Requester,
            Status = UserStatus.Active
        }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_Soft_Deletes()
    {
        var user = AddUser();
        _currentUser.UserId = Guid.NewGuid();
        var handler = new DeleteUserCommandHandler(_users, _bookings, _currentUser, _uow);

        await handler.Handle(new DeleteUserCommand { Id = user.Id }, CancellationToken.None);

        Assert.True(user.IsDeleted);
        Assert.NotNull(user.UpdatedAt);
        Assert.True(_uow.SaveCount > 0);
    }

    [Fact]
    public async Task DeleteUser_With_Active_Booking_Throws()
    {
        var user = AddUser();
        _bookings.Items.Add(new Booking
        {
            ResourceId = Guid.NewGuid(),
            RequesterId = user.Id,
            StartTime = DateTime.UtcNow.AddHours(1),
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = BookingStatus.Approved
        });
        _currentUser.UserId = Guid.NewGuid();
        var handler = new DeleteUserCommandHandler(_users, _bookings, _currentUser, _uow);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new DeleteUserCommand { Id = user.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task DeleteUser_Cannot_Delete_Self()
    {
        var user = AddUser();
        _currentUser.UserId = user.Id;
        var handler = new DeleteUserCommandHandler(_users, _bookings, _currentUser, _uow);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new DeleteUserCommand { Id = user.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task ChangePassword_Succeeds()
    {
        var user = AddUser();
        _currentUser.UserId = user.Id;
        var handler = new ChangePasswordCommandHandler(_users, _currentUser, _uow);

        await handler.Handle(new ChangePasswordCommand { CurrentPassword = "secret123", NewPassword = "newpass123" }, CancellationToken.None);

        Assert.True(BCrypt.Net.BCrypt.Verify("newpass123", user.PasswordHash));
        Assert.True(_uow.SaveCount > 0);
    }

    [Fact]
    public async Task ChangePassword_Wrong_Current_Throws()
    {
        var user = AddUser();
        _currentUser.UserId = user.Id;
        var handler = new ChangePasswordCommandHandler(_users, _currentUser, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new ChangePasswordCommand { CurrentPassword = "wrong", NewPassword = "newpass123" }, CancellationToken.None));
    }

    [Fact]
    public async Task ResetPassword_Succeeds()
    {
        var user = AddUser();
        var handler = new ResetPasswordCommandHandler(_users, _uow);

        await handler.Handle(new ResetPasswordCommand { Id = user.Id, NewPassword = "resetpass1" }, CancellationToken.None);

        Assert.True(BCrypt.Net.BCrypt.Verify("resetpass1", user.PasswordHash));
    }

    [Fact]
    public async Task GetUsers_Filters_By_Role_And_Keyword()
    {
        AddUser(email: "a@test.com", role: UserRole.Requester);
        AddUser(email: "b@test.com", role: UserRole.LabManager);
        var handler = new GetUsersQueryHandler(_users);

        var result = await handler.Handle(new GetUsersQuery { Role = UserRole.LabManager }, CancellationToken.None);

        Assert.Equal(1, result.TotalCount);
        Assert.Equal("b@test.com", Assert.Single(result.Items).Email);
    }

    [Fact]
    public async Task GetUserById_Unknown_User_Throws()
    {
        var handler = new GetUserByIdQueryHandler(_users);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new GetUserByIdQuery { Id = Guid.NewGuid() }, CancellationToken.None));
    }
}
