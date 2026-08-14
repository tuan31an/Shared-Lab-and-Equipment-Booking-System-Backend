using LabBooking.Application.Features.Violations;
using LabBooking.Domain.Entities;
using LabBooking.Domain.Enums;
using Xunit;

namespace LabBooking.Tests;

public class ViolationSweeperTests
{
    private static DateTime EndedMinutesAgo(int minutes) => DateTime.UtcNow.AddMinutes(-minutes);

    private readonly FakeRepository<Booking> _bookings = new();
    private readonly FakeRepository<CheckInOut> _checkInOuts = new();
    private readonly FakeRepository<Violation> _violations = new();
    private readonly FakeRepository<Restriction> _restrictions = new();
    private readonly FakeRepository<User> _users = new();
    private readonly FakeUnitOfWork _uow = new();

    private Booking AddDueBooking(Guid requesterId, int endedMinutesAgo = 60)
    {
        var booking = new Booking
        {
            ResourceId = Guid.NewGuid(),
            RequesterId = requesterId,
            StartTime = EndedMinutesAgo(endedMinutesAgo + 60),
            EndTime = EndedMinutesAgo(endedMinutesAgo),
            Purpose = "test",
            Status = BookingStatus.Approved
        };
        _bookings.Items.Add(booking);
        return booking;
    }

    private Task<int> Sweep() => ViolationSweeper.SweepAsync(
        _bookings, _checkInOuts, _violations, _restrictions, _users, _uow, TestConfig.Empty(), CancellationToken.None);

    [Fact]
    public async Task Sweep_Records_NoShow_For_Unchecked_Approved_Booking()
    {
        var booking = AddDueBooking(Guid.NewGuid());

        var count = await Sweep();

        Assert.Equal(1, count);
        var violation = Assert.Single(_violations.Items);
        Assert.Equal(ViolationType.NoShow, violation.Type);
        Assert.Equal(booking.RequesterId, violation.UserId);
        Assert.Equal(booking.Id, violation.BookingId);
    }

    [Fact]
    public async Task Sweep_Skips_CheckedIn_Booking()
    {
        var booking = AddDueBooking(Guid.NewGuid());
        _checkInOuts.Items.Add(new CheckInOut { BookingId = booking.Id, CheckInTime = DateTime.UtcNow.AddMinutes(-59) });

        var count = await Sweep();

        Assert.Equal(0, count);
        Assert.Empty(_violations.Items);
    }

    [Fact]
    public async Task Sweep_Does_Not_Duplicate_Already_Recorded_NoShow()
    {
        var booking = AddDueBooking(Guid.NewGuid());
        _violations.Items.Add(new Violation
        {
            UserId = booking.RequesterId,
            BookingId = booking.Id,
            Type = ViolationType.NoShow,
            RecordedAt = DateTime.UtcNow
        });

        var count = await Sweep();

        Assert.Equal(0, count);
        Assert.Single(_violations.Items);
    }

    [Fact]
    public async Task Sweep_Respects_NoShow_Grace_Period()
    {
        AddDueBooking(Guid.NewGuid(), endedMinutesAgo: 10);

        var count = await Sweep();

        Assert.Equal(0, count);
        Assert.Empty(_violations.Items);
    }

    [Fact]
    public async Task Sweep_AutoRestricts_Offender_At_Threshold()
    {
        var user = new User { FullName = "Offender", Email = "o@e.com", Status = UserStatus.Active };
        _users.Items.Add(user);

        // 2 vi phạm cũ (booking khác) + 1 no-show mới trong sweep = đạt ngưỡng 3.
        _violations.Items.Add(new Violation { UserId = user.Id, BookingId = Guid.NewGuid(), Type = ViolationType.NoShow, RecordedAt = DateTime.UtcNow });
        _violations.Items.Add(new Violation { UserId = user.Id, BookingId = Guid.NewGuid(), Type = ViolationType.NoShow, RecordedAt = DateTime.UtcNow });
        AddDueBooking(user.Id);

        var count = await Sweep();

        Assert.Equal(1, count);
        var restriction = Assert.Single(_restrictions.Items);
        Assert.Equal(user.Id, restriction.UserId);
        Assert.Equal(UserStatus.Restricted, user.Status);
    }

    [Fact]
    public async Task Sweep_Does_Not_Restrict_Below_Threshold()
    {
        var user = new User { Status = UserStatus.Active };
        _users.Items.Add(user);
        AddDueBooking(user.Id);

        var count = await Sweep();

        Assert.Equal(1, count);
        Assert.Empty(_restrictions.Items);
        Assert.Equal(UserStatus.Active, user.Status);
    }
}