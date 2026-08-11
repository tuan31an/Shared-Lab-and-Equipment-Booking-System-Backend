using LabBooking.API.Controllers;
using LabBooking.API.Models;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Auth.Commands;
using LabBooking.Application.Features.Bookings.Commands;
using LabBooking.Application.Features.Maintenances.Commands;
using LabBooking.Application.Features.Waitlists.Commands;
using LabBooking.Application.Features.Waitlists.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace LabBooking.Tests;

public class ControllerTests
{
    private static readonly WaitlistDto SomeWaitlist = new(Guid.NewGuid(), Guid.NewGuid(), "Lab A", Guid.NewGuid(), DateTime.UtcNow, DateTime.UtcNow, "Waiting", null, DateTime.UtcNow);
    private static readonly BookingDto SomeBooking = new(Guid.NewGuid(), Guid.NewGuid(), "Lab A", Guid.NewGuid(), null, null, null, DateTime.UtcNow, DateTime.UtcNow, "p", "Pending", null, null, null, null, null, DateTime.UtcNow);
    private static readonly BookingConflictResponse SomeConflict = new(true, [], []);

    private static (TController Controller, FakeSender Sender) Create<TController>(Func<ISender, TController> factory)
    {
        var sender = new FakeSender();
        return (factory(sender), sender);
    }

    [Fact]
    public async Task Waitlists_Join_Returns_Created_And_Sends_Command()
    {
        var (controller, sender) = Create(s => new WaitlistsController(s));
        sender.Register<JoinWaitlistCommand>(SomeWaitlist);
        var command = new JoinWaitlistCommand();

        var result = await controller.Join(command);

        Assert.IsType<CreatedResult>(result);
        Assert.Same(command, sender.Sent.Single());
    }

    [Fact]
    public async Task Waitlists_Leave_Returns_Ok_Even_When_Command_Body_Null()
    {
        var (controller, sender) = Create(s => new WaitlistsController(s));
        sender.Register<LeaveWaitlistCommand>(SomeWaitlist);

        var result = await controller.Leave(Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
        var sent = Assert.IsType<LeaveWaitlistCommand>(sender.Sent.Single());
        Assert.NotEqual(Guid.Empty, sent.WaitlistId);
    }

    [Fact]
    public async Task Bookings_Create_Returns_Created()
    {
        var (controller, sender) = Create(s => new BookingsController(s));
        sender.Register<CreateBookingCommand>(SomeBooking);

        var result = await controller.Create(new CreateBookingCommand());

        Assert.IsType<CreatedResult>(result);
        Assert.IsType<CreateBookingCommand>(sender.Sent.Single());
    }

    [Fact]
    public async Task Bookings_CheckConflict_Returns_Ok_With_Payload()
    {
        var (controller, sender) = Create(s => new BookingsController(s));
        sender.Register<CheckBookingConflictCommand>(SomeConflict);

        var result = Assert.IsType<OkObjectResult>(await controller.CheckConflict(new CheckBookingConflictCommand()));

        Assert.Same(SomeConflict, result.Value);
    }

    [Fact]
    public async Task Bookings_Reject_Fills_BookingId_From_Route()
    {
        var (controller, sender) = Create(s => new BookingsController(s));
        sender.Register<RejectBookingCommand>(SomeBooking);
        var id = Guid.NewGuid();

        await controller.Reject(id, null);

        var sent = Assert.IsType<RejectBookingCommand>(sender.Sent.Single());
        Assert.Equal(id, sent.BookingId);
    }

    [Fact]
    public async Task Auth_Login_Returns_Ok_Refresh_Returns_Ok_Register_Created()
    {
        var (controller, sender) = Create(s => new AuthController(s));
        var response = new AuthResponse("access", "refresh", 3600,
            new UserDto(Guid.NewGuid(), "Alice", "a@b.com", "Requester", "Active", DateTime.UtcNow));
        sender.Register<LoginCommand>(response)
            .Register<RefreshCommand>(response)
            .Register<RegisterCommand>(response.User);

        Assert.IsType<OkObjectResult>(await controller.Login(new LoginCommand()));
        Assert.IsType<OkObjectResult>(await controller.Refresh(new RefreshCommand()));
        Assert.IsType<CreatedResult>(await controller.Register(new RegisterCommand()));
        Assert.Equal(3, sender.Sent.Count);
    }

    [Fact]
    public async Task Auth_Logout_With_Empty_Token_Returns_BadRequest()
    {
        var (controller, sender) = Create(s => new AuthController(s));

        var result = Assert.IsType<BadRequestObjectResult>(await controller.Logout(null));
        var payload = Assert.IsAssignableFrom<ApiResponse>(result.Value);
        Assert.False(payload.IsSuccess);
        Assert.Contains("RefreshToken is required.", payload.ErrorMessages);
        Assert.Empty(sender.Sent);
    }

    [Fact]
    public async Task Auth_Logout_With_Token_Returns_NoContent()
    {
        var (controller, sender) = Create(s => new AuthController(s));
        sender.Register<LogoutCommand>(Unit.Value);

        var result = await controller.Logout(new LogoutCommand { RefreshToken = "token" });

        Assert.IsType<NoContentResult>(result);
        Assert.Single(sender.Sent);
    }

    [Fact]
    public async Task Resources_Delete_Returns_NoContent_And_Delete_Returns_Ok()
    {
        var (controller, sender) = Create(s => new ResourcesController(s));
        sender.Register<LabBooking.Application.Features.Resources.Commands.DeleteResourceCommand>(Unit.Value);
        sender.Register<LabBooking.Application.Features.Resources.Queries.GetResourcesQuery>(new PaginationResponse<ResourceDto>([], 0, 1, 20));

        Assert.IsType<NoContentResult>(await controller.Delete(Guid.NewGuid()));
        Assert.IsType<OkObjectResult>(await controller.List(new LabBooking.Application.Features.Resources.Queries.GetResourcesQuery()));
    }

    [Fact]
    public async Task Maintenances_Resolve_With_Null_Body_Builds_Command()
    {
        var (controller, sender) = Create(s => new MaintenancesController(s));
        sender.Register<ResolveMaintenanceCommand>(new MaintenanceDto(Guid.NewGuid(), Guid.NewGuid(), "Lab A", DateTime.UtcNow, DateTime.UtcNow, null, null, "Scheduled", null));
        var id = Guid.NewGuid();

        var result = await controller.Resolve(id, null);

        Assert.IsType<OkObjectResult>(result);
        var sent = Assert.IsType<ResolveMaintenanceCommand>(sender.Sent.Single());
        Assert.Equal(id, sent.MaintenanceId);
    }
}