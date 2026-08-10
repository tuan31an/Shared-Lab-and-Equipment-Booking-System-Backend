using LabBooking.Domain.Scheduling;
using Xunit;

namespace LabBooking.Tests;

public class SchedulingTests
{
    private static DateTime D(int day, int hour) => new DateTime(2026, 8, 10).AddDays(day).AddHours(hour);

    [Fact]
    public void Overlap_Detects_Overlapping_Ranges()
    {
        Assert.True(Scheduling.IsOverlap(D(0, 9), D(0, 11), D(0, 10), D(0, 12)));
        Assert.True(Scheduling.IsOverlap(D(0, 10), D(0, 12), D(0, 9), D(0, 11)));
        Assert.False(Scheduling.IsOverlap(D(0, 9), D(0, 10), D(0, 10), D(0, 11)));
    }

    [Fact]
    public void Merge_Folds_Overlapping_And_Adjacent_Intervals()
    {
        var merged = Scheduling.Merge(new[]
        {
            (D(0, 9), D(0, 11)),
            (D(0, 10), D(0, 12)),
            (D(0, 14), D(0, 16))
        });

        Assert.Equal(2, merged.Count);
        Assert.Equal((D(0, 9), D(0, 12)), merged[0]);
        Assert.Equal((D(0, 14), D(0, 16)), merged[1]);
    }

    [Fact]
    public void FreeGaps_Returns_Holes_Between_Busy_Intervals()
    {
        var busy = new[]
        {
            (D(0, 9), D(0, 11)),
            (D(0, 15), D(0, 17))
        };

        var gaps = Scheduling.FreeGaps(D(0, 0), D(1, 0), busy);

        Assert.Contains((D(0, 0), D(0, 9)), gaps);
        Assert.Contains((D(0, 11), D(0, 15)), gaps);
        Assert.Contains((D(0, 17), D(1, 0)), gaps);
    }

    [Fact]
    public void SuggestSlots_Returns_Three_Nearest_Alternatives()
    {
        // Muốn đặt 9:00 → 11:00, nhưng 9-11 đã bận.
        var busy = new[] { (D(0, 9), D(0, 11)) };
        var requestedStart = D(0, 9);
        var duration = TimeSpan.FromHours(2);

        var slots = Scheduling.SuggestSlots(
            Scheduling.FreeGaps(D(0, 7), D(0, 18), busy),
            requestedStart,
            duration);

        Assert.True(slots.Count >= 3, $"Expected at least 3 slots, got {slots.Count}");
        Assert.All(slots, s => Assert.True(s.End - s.Start == duration));
        Assert.All(slots, s => Assert.True(s.Start < D(0, 9) || s.Start >= D(0, 11)));
        // Gần 9:00 nhất: 7:00-9:00.
        Assert.Equal((D(0, 7), D(0, 9)), slots[0]);
    }
}