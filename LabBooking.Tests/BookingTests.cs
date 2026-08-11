using LabBooking.Application.Common.Exceptions;
using LabBooking.Application.Contracts;
using LabBooking.Application.Features.Bookings.Commands;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Xunit;

namespace LabBooking.Tests;

public class BookingTests
{
    private static DateTime InFuture(double hours = 24) => DateTime.UtcNow.AddHours(hours);

    private readonly FakeRepository<Booking> _bookings = new();
    private readonly FakeRepository<Resource> _resources = new();
    private readonly FakeRepository<User> _users = new();
    private readonly FakeRepository<PriorityRule> _rules = new();
    private readonly FakeRepository<Maintenance> _maintenances = new();
    private readonly FakeRepository<Restriction> _restrictions = new();
    private readonly FakeRepository<CheckInOut> _checkInOuts = new();
    private readonly FakeRepository<Violation> _violations = new();
    private readonly FakeRepository<Waitlist> _waitlists = new();
    private readonly FakeRepository<Notification> _notifications = new();
    private readonly FakeCurrentUser _user = new() { UserId = Guid.NewGuid(), Role = "Requester" };
    private readonly FakeUnitOfWork _uow = new();

    private Resource AddResource()
    {
        var resource = new Resource { Name = "Lab A", LabManagerId = Guid.NewGuid() };
        _resources.Items.Add(resource);
        return resource;
    }

    private void AddRequester()
    {
        var user = new User { FullName = "Requester", Email = "req@test.com", PasswordHash = "x", Role = UserRole.Requester, Status = UserStatus.Active };
        _users.Items.Add(user);
        _user.UserId = user.Id;
    }

    private Booking AddBooking(Resource resource, DateTime start, DateTime end, BookingStatus status = BookingStatus.Pending, Guid? ruleId = null)
    {
        var booking = new Booking
        {
            ResourceId = resource.Id,
            RequesterId = _user.UserId!.Value,
            StartTime = start,
            EndTime = end,
            Purpose = "test",
            Status = status,
            RuleId = ruleId
        };
        _bookings.Items.Add(booking);
        return booking;
    }

    [Fact]
    public async Task Create_Creates_Pending_Booking()
    {
        var resource = AddResource();
        AddRequester();
        var handler = new CreateBookingCommandHandler(_bookings, _resources, _users, _rules, _maintenances, _restrictions, _user, _uow);

        var dto = await handler.Handle(new CreateBookingCommand
        {
            ResourceId = resource.Id,
            StartTime = InFuture(2),
            EndTime = InFuture(4),
            Purpose = "Do research"
        }, CancellationToken.None);

        Assert.Equal(BookingStatus.Pending.ToString(), dto.Status);
        Assert.Equal("Do research", dto.Purpose);
        Assert.Equal(resource.Name, dto.ResourceName);
        Assert.Single(_bookings.Items);
    }

    [Fact]
    public async Task Create_End_Before_Start_Throws()
    {
        var resource = AddResource();
        var handler = new CreateBookingCommandHandler(_bookings, _resources, _users, _rules, _maintenances, _restrictions, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CreateBookingCommand
        {
            ResourceId = resource.Id,
            StartTime = InFuture(2),
            EndTime = InFuture(1),
            Purpose = "x"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task Create_Conflict_With_Pending_Throws_And_Suggests()
    {
        var resource = AddResource();
        AddRequester();
        var start = InFuture(4);
        var end = start.AddHours(2);
        AddBooking(resource, start, end, BookingStatus.Pending);
        var handler = new CreateBookingCommandHandler(_bookings, _resources, _users, _rules, _maintenances, _restrictions, _user, _uow);

        var ex = await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new CreateBookingCommand
        {
            ResourceId = resource.Id,
            StartTime = start.AddMinutes(30),
            EndTime = end.AddMinutes(30),
            Purpose = "x"
        }, CancellationToken.None));

        var payload = Assert.IsType<BookingConflictResponse>(ex.Payload);
        Assert.True(payload.HasConflict);
        Assert.Single(payload.ConflictingBookings);
        Assert.NotEmpty(payload.SuggestedSlots);
    }

    [Fact]
    public async Task Create_Rejected_Booking_Does_Not_Conflict()
    {
        var resource = AddResource();
        AddRequester();
        var start = InFuture(4);
        var end = start.AddHours(2);
        AddBooking(resource, start, end, BookingStatus.Rejected);
        var handler = new CreateBookingCommandHandler(_bookings, _resources, _users, _rules, _maintenances, _restrictions, _user, _uow);

        var dto = await handler.Handle(new CreateBookingCommand
        {
            ResourceId = resource.Id,
            StartTime = start.AddMinutes(30),
            EndTime = end.AddMinutes(30),
            Purpose = "x"
        }, CancellationToken.None);

        Assert.Equal(BookingStatus.Pending.ToString(), dto.Status);
        Assert.Equal(2, _bookings.Items.Count);
    }

    [Fact]
    public async Task Create_With_Active_Restriction_Throws()
    {
        var resource = AddResource();
        AddRequester();
        var start = InFuture(4);
        var end = start.AddHours(2);
        AddBooking(resource, start, end);
        var today = DateTime.UtcNow.Date;
        _restrictions.Items.Add(new Restriction
        {
            UserId = _user.UserId!.Value,
            StartDate = today.AddDays(-1),
            EndDate = today.AddDays(7),
            Reason = "3 violations"
        });

        var handler = new CreateBookingCommandHandler(_bookings, _resources, _users, _rules, _maintenances, _restrictions, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CreateBookingCommand
        {
            ResourceId = resource.Id,
            StartTime = InFuture(5),
            EndTime = InFuture(7),
            Purpose = "x"
        }, CancellationToken.None));
    }

    [Fact]
    public async Task CheckConflict_No_Conflict_Returns_False()
    {
        var resource = AddResource();
        var handler = new CheckBookingConflictCommandHandler(_bookings, _resources, _users, _rules, _maintenances);

        var response = await handler.Handle(new CheckBookingConflictCommand
        {
            ResourceId = resource.Id,
            StartTime = InFuture(4),
            EndTime = InFuture(6)
        }, CancellationToken.None);

        Assert.False(response.HasConflict);
        Assert.Empty(response.ConflictingBookings);
    }

    [Fact]
    public async Task CheckConflict_With_Maintenance_Returns_True()
    {
        var resource = AddResource();
        var start = InFuture(4);
        var end = start.AddHours(2);
        _maintenances.Items.Add(new Maintenance
        {
            ResourceId = resource.Id,
            StartTime = start.AddMinutes(-30),
            EndTime = end.AddMinutes(30),
            Status = MaintenanceStatus.Scheduled
        });
        var handler = new CheckBookingConflictCommandHandler(_bookings, _resources, _users, _rules, _maintenances);

        var response = await handler.Handle(new CheckBookingConflictCommand
        {
            ResourceId = resource.Id,
            StartTime = start,
            EndTime = end
        }, CancellationToken.None);

        Assert.True(response.HasConflict);
    }

    [Fact]
    public async Task Cancel_Deletes_Slot_And_Notifies_Waitlist()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, InFuture(24), InFuture(26), BookingStatus.Approved);
        var otherUser = Guid.NewGuid();
        _waitlists.Items.Add(new Waitlist
        {
            ResourceId = resource.Id,
            RequesterId = otherUser,
            DesiredStart = booking.StartTime,
            DesiredEnd = booking.EndTime,
            Status = WaitlistStatus.Waiting
        });

        var handler = new CancelBookingCommandHandler(
            _bookings, _resources, _users, _rules, _checkInOuts, _waitlists, _notifications, _user, TestConfig.Empty(), _uow);

        var dto = await handler.Handle(new CancelBookingCommand { BookingId = booking.Id }, CancellationToken.None);

        Assert.Equal(BookingStatus.Cancelled.ToString(), dto.Status);
        Assert.Equal(BookingStatus.Cancelled, booking.Status);
        var entry = Assert.Single(_waitlists.Items);
        Assert.Equal(WaitlistStatus.Notified, entry.Status);
        Assert.NotNull(entry.NotifiedAt);
        var notification = Assert.Single(_notifications.Items);
        Assert.Equal(NotificationType.WaitlistAvailable, notification.Type);
        Assert.Equal(otherUser, notification.UserId);
    }

    [Fact]
    public async Task Cancel_Too_Close_To_Start_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, InFuture(1), InFuture(3), BookingStatus.Approved);
        var handler = new CancelBookingCommandHandler(
            _bookings, _resources, _users, _rules, _checkInOuts, _waitlists, _notifications, _user, TestConfig.Empty(), _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CancelBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task Cancel_Another_Users_Booking_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, InFuture(24), InFuture(26), BookingStatus.Approved);
        booking.RequesterId = Guid.NewGuid();
        var other = new FakeCurrentUser { UserId = Guid.NewGuid(), Role = "Requester" };
        var handler = new CancelBookingCommandHandler(
            _bookings, _resources, _users, _rules, _checkInOuts, _waitlists, _notifications, other, TestConfig.Empty(), _uow);

        await Assert.ThrowsAsync<UnauthorizedException>(() => handler.Handle(new CancelBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task CheckIn_Sets_CheckIn_Time()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(30), BookingStatus.Approved);
        var handler = new CheckInBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _user, _uow);

        var dto = await handler.Handle(new CheckInBookingCommand { BookingId = booking.Id }, CancellationToken.None);

        Assert.NotNull(dto.CheckInTime);
        Assert.Single(_checkInOuts.Items);
        Assert.Equal(booking.Id, _checkInOuts.Items[0].BookingId);
    }

    [Fact]
    public async Task CheckIn_Too_Early_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, InFuture(24), InFuture(26), BookingStatus.Approved);
        var handler = new CheckInBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CheckInBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task CheckIn_NonApproved_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, DateTime.UtcNow.AddMinutes(-30), DateTime.UtcNow.AddMinutes(30), BookingStatus.Pending);
        var handler = new CheckInBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _user, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CheckInBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task CheckOut_OnTime_Completes_Without_Violation()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, DateTime.UtcNow.AddMinutes(-60), DateTime.UtcNow.AddMinutes(-5), BookingStatus.Approved);
        _checkInOuts.Items.Add(new CheckInOut { BookingId = booking.Id, CheckInTime = DateTime.UtcNow.AddMinutes(-10) });
        var handler = new CheckOutBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _violations, _user, _uow, TestConfig.Empty());

        var dto = await handler.Handle(new CheckOutBookingCommand { BookingId = booking.Id }, CancellationToken.None);

        Assert.Equal(BookingStatus.Completed.ToString(), dto.Status);
        Assert.Equal(BookingStatus.Completed, booking.Status);
        Assert.Null(_violations.Items.FirstOrDefault(v => v.Type == ViolationType.Late));
    }

    [Fact]
    public async Task CheckOut_Overdue_Creates_Late_Violation()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, DateTime.UtcNow.AddMinutes(-120), DateTime.UtcNow.AddMinutes(-60), BookingStatus.Approved);
        _checkInOuts.Items.Add(new CheckInOut { BookingId = booking.Id, CheckInTime = DateTime.UtcNow.AddMinutes(-100) });
        var handler = new CheckOutBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _violations, _user, _uow, TestConfig.Empty());

        await handler.Handle(new CheckOutBookingCommand { BookingId = booking.Id }, CancellationToken.None);

        var violation = Assert.Single(_violations.Items);
        Assert.Equal(ViolationType.Late, violation.Type);
        Assert.Equal(booking.RequesterId, violation.UserId);
        Assert.Equal(booking.Id, violation.BookingId);
    }

    [Fact]
    public async Task CheckOut_Without_CheckIn_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, DateTime.UtcNow.AddMinutes(-60), DateTime.UtcNow.AddMinutes(30), BookingStatus.Approved);
        var handler = new CheckOutBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _violations, _user, _uow, TestConfig.Empty());

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new CheckOutBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task Approve_NonPending_Throws()
    {
        var resource = AddResource();
        var booking = AddBooking(resource, InFuture(24), InFuture(26), BookingStatus.Approved);
        var admin = new FakeCurrentUser { UserId = Guid.NewGuid(), Role = "Admin" };
        var handler = new ApproveBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _maintenances, admin, _uow);

        await Assert.ThrowsAsync<ArgumentException>(() => handler.Handle(new ApproveBookingCommand { BookingId = booking.Id }, CancellationToken.None));
    }

    [Fact]
    public async Task Approve_With_Higher_Priority_Retracts_Lower_Priority()
    {
        var resource = AddResource();
        var winnerRule = new PriorityRule { Name = "Research", PriorityLevel = 1 };
        var loserRule = new PriorityRule { Name = "Study", PriorityLevel = 2 };
        _rules.Items.Add(winnerRule);
        _rules.Items.Add(loserRule);

        var start = InFuture(24);
        var end = start.AddHours(2);
        var winner = AddBooking(resource, start, end, BookingStatus.Pending, winnerRule.Id);
        var loser = AddBooking(resource, start.AddMinutes(30), end.AddMinutes(30), BookingStatus.Approved, loserRule.Id);

        var admin = new FakeCurrentUser { UserId = Guid.NewGuid(), Role = "Admin" };
        var handler = new ApproveBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _maintenances, admin, _uow);

        var dto = await handler.Handle(new ApproveBookingCommand { BookingId = winner.Id }, CancellationToken.None);

        Assert.Equal(BookingStatus.Approved.ToString(), dto.Status);
        Assert.Equal(BookingStatus.Approved, winner.Status);
        Assert.Equal(BookingStatus.Rejected, loser.Status);
    }

    [Fact]
    public async Task Approve_With_Equal_Priority_Conflict_Throws()
    {
        var resource = AddResource();
        var rule = new PriorityRule { Name = "Research", PriorityLevel = 1 };
        _rules.Items.Add(rule);

        var start = InFuture(24);
        var end = start.AddHours(2);
        var winner = AddBooking(resource, start, end, BookingStatus.Pending, rule.Id);
        AddBooking(resource, start.AddMinutes(30), end.AddMinutes(30), BookingStatus.Approved, rule.Id);

        var admin = new FakeCurrentUser { UserId = Guid.NewGuid(), Role = "Admin" };
        var handler = new ApproveBookingCommandHandler(_bookings, _resources, _users, _rules, _checkInOuts, _maintenances, admin, _uow);

        await Assert.ThrowsAsync<ConflictException>(() => handler.Handle(new ApproveBookingCommand { BookingId = winner.Id }, CancellationToken.None));
    }
}