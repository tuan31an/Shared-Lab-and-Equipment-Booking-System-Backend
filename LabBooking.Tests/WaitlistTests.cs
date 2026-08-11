using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Features.Waitlists.Commands;
using LabBooking.Application.Features.Waitlists.Queries;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Xunit;

namespace LabBooking.Tests;

public class WaitlistTests
{
    private static DateTime InFuture(int hours = 24) => DateTime.UtcNow.AddHours(hours);

    private readonly FakeRepository<Resource> _resources = new();
    private readonly FakeRepository<Waitlist> _waitlists = new();
    private readonly FakeRepository<Notification> _notifications = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.NewGuid(), Role = "Requester" };
    private readonly FakeUnitOfWork _uow = new();

    private Resource AddResource(string name = "Lab A")
    {
        var resource = new Resource { Name = name };
        _resources.Items.Add(resource);
        return resource;
    }

    [Fact]
    public async Task Join_Creates_Waiting_Entry_And_Returns_Dto()
    {
        var resource = AddResource();
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, _user, _uow);

        var dto = await handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = resource.Id,
            DesiredStart = InFuture(),
            DesiredEnd = InFuture(25)
        }, CancellationToken.None);

        Assert.Equal(WaitlistStatus.Waiting.ToString(), dto.Status);
        Assert.Equal(resource.Name, dto.ResourceName);
        Assert.Equal(_user.UserId, dto.RequesterId);
        Assert.Single(_waitlists.Items);
    }

    [Fact]
    public async Task Join_End_Before_Start_Throws()
    {
        var resource = AddResource();
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = resource.Id,
            DesiredStart = InFuture(25),
            DesiredEnd = InFuture(24)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Join_Start_In_Past_Throws()
    {
        var resource = AddResource();
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = resource.Id,
            DesiredStart = DateTime.UtcNow.AddHours(-1),
            DesiredEnd = InFuture()
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Join_Missing_Resource_Throws_NotFound()
    {
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, _user, _uow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = Guid.NewGuid(),
            DesiredStart = InFuture(),
            DesiredEnd = InFuture(25)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Join_Without_Authenticated_User_Throws()
    {
        var resource = AddResource();
        var anon = new FakeCurrentUser();
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, anon, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = resource.Id,
            DesiredStart = InFuture(),
            DesiredEnd = InFuture(25)
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Join_Overlapping_Active_Entry_Throws_Conflict()
    {
        var resource = AddResource();
        var handler = new JoinWaitlistCommandHandler(_resources, _waitlists, _user, _uow);
        var start = InFuture();
        var end = InFuture(25);

        await handler.Handle(new JoinWaitlistCommand { ResourceId = resource.Id, DesiredStart = start, DesiredEnd = end }, CancellationToken.None);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new JoinWaitlistCommand
        {
            ResourceId = resource.Id,
            DesiredStart = start.AddMinutes(30),
            DesiredEnd = end
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Leave_Sets_Expired_And_Returns_Dto()
    {
        var resource = AddResource();
        var entry = new Waitlist
        {
            ResourceId = resource.Id,
            RequesterId = _user.UserId!.Value,
            DesiredStart = InFuture(),
            DesiredEnd = InFuture(25)
        };
        _waitlists.Items.Add(entry);

        var handler = new LeaveWaitlistCommandHandler(_waitlists, _resources, _user, _uow);
        var dto = await handler.Handle(new LeaveWaitlistCommand { WaitlistId = entry.Id }, CancellationToken.None);

        Assert.Equal(WaitlistStatus.Expired, entry.Status);
        Assert.Equal(WaitlistStatus.Expired.ToString(), dto.Status);
        Assert.Equal(resource.Name, dto.ResourceName);
    }

    [Fact]
    public async Task Leave_Missing_Entry_Throws_NotFound()
    {
        var handler = new LeaveWaitlistCommandHandler(_waitlists, _resources, _user, _uow);

        await Assert.ThrowsAsync<NotFoundException>(() => handler.Handle(new LeaveWaitlistCommand { WaitlistId = Guid.NewGuid() }, CancellationToken.None));
    }

    [Fact]
    public async Task Leave_Another_Users_Entry_By_NonAdmin_Throws()
    {
        var entry = new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = Guid.NewGuid(), DesiredStart = InFuture(), DesiredEnd = InFuture(25) };
        _waitlists.Items.Add(entry);
        var handler = new LeaveWaitlistCommandHandler(_waitlists, _resources, _user, _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new LeaveWaitlistCommand { WaitlistId = entry.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task Leave_Notified_Entry_Throws()
    {
        var entry = new Waitlist
        {
            ResourceId = Guid.NewGuid(),
            RequesterId = _user.UserId!.Value,
            DesiredStart = InFuture(),
            DesiredEnd = InFuture(25),
            Status = WaitlistStatus.Notified
        };
        _waitlists.Items.Add(entry);
        var handler = new LeaveWaitlistCommandHandler(_waitlists, _resources, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new LeaveWaitlistCommand { WaitlistId = entry.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task GetMy_NonAdmin_Sees_Only_Own()
    {
        var mine = new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = _user.UserId!.Value, DesiredStart = InFuture(), DesiredEnd = InFuture(25) };
        var theirs = new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = Guid.NewGuid(), DesiredStart = InFuture(), DesiredEnd = InFuture(25) };
        _waitlists.Items.Add(mine);
        _waitlists.Items.Add(theirs);

        var handler = new GetMyWaitlistsQueryHandler(_waitlists, _resources, _user);
        var result = await handler.Handle(new GetMyWaitlistsQuery(), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(mine.Id, result[0].Id);
    }

    [Fact]
    public async Task GetMy_ActiveOnly_Filters_Waiting()
    {
        var waiting = new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = _user.UserId!.Value, DesiredStart = InFuture(), DesiredEnd = InFuture(25), Status = WaitlistStatus.Waiting };
        var notified = new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = _user.UserId!.Value, DesiredStart = InFuture(), DesiredEnd = InFuture(25), Status = WaitlistStatus.Notified };
        _waitlists.Items.Add(waiting);
        _waitlists.Items.Add(notified);

        var handler = new GetMyWaitlistsQueryHandler(_waitlists, _resources, _user);
        var result = await handler.Handle(new GetMyWaitlistsQuery { ActiveOnly = true }, CancellationToken.None);

        Assert.Single(result);
        Assert.Equal(WaitlistStatus.Waiting.ToString(), result[0].Status);
    }

    [Fact]
    public async Task GetMy_Admin_Sees_All()
    {
        var admin = new FakeCurrentUser { UserId = Guid.NewGuid(), Role = "Admin" };
        _waitlists.Items.Add(new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = Guid.NewGuid(), DesiredStart = InFuture(), DesiredEnd = InFuture(25) });
        _waitlists.Items.Add(new Waitlist { ResourceId = Guid.NewGuid(), RequesterId = Guid.NewGuid(), DesiredStart = InFuture(), DesiredEnd = InFuture(25) });

        var handler = new GetMyWaitlistsQueryHandler(_waitlists, _resources, admin);
        var result = await handler.Handle(new GetMyWaitlistsQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }
}
