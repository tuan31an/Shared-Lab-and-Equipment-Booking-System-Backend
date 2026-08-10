namespace LabBooking.Domain.Scheduling
{
    /// <summary>
    /// Logic thuần về khung giờ: phòng chồng lấn, gộp khoảng bận, tạo khung trống
    /// và đề xuất khung thay thế. Không phụ thuộc framework để có thể kiểm thử độc lập.
    /// </summary>
    public static class Scheduling
    {
        public static bool IsOverlap(DateTime aStart, DateTime aEnd, DateTime bStart, DateTime bEnd)
            => aStart < bEnd && bStart < aEnd;

        /// <summary>Sắp xếp và gộp các khoảng chồng lấn/thành phần giao nhau.</summary>
        public static IReadOnlyList<(DateTime Start, DateTime End)> Merge(
            IEnumerable<(DateTime Start, DateTime End)> intervals)
        {
            var sorted = intervals
                .Where(i => i.End > i.Start)
                .OrderBy(i => i.Start)
                .ThenBy(i => i.End)
                .ToList();

            var merged = new List<(DateTime Start, DateTime End)>();
            foreach (var item in sorted)
            {
                if (merged.Count > 0 && item.Start < merged[^1].End)
                {
                    if (item.End > merged[^1].End)
                        merged[^1] = (merged[^1].Start, item.End);
                }
                else
                {
                    merged.Add(item);
                }
            }

            return merged;
        }

        /// <summary>
        /// Khoảng trống trong cửa sổ [windowStart, windowEnd] sau khi khấu trừ khoảng bận.
        /// Nếu có operating hours, giới hạn theo khung giờ hoạt động mỗi ngày.
        /// </summary>
        public static IReadOnlyList<(DateTime Start, DateTime End)> FreeGaps(
            DateTime windowStart,
            DateTime windowEnd,
            IReadOnlyList<(DateTime Start, DateTime End)> busy,
            TimeSpan? openFrom = null,
            TimeSpan? openUntil = null)
        {
            var cursor = windowStart;
            var gaps = new List<(DateTime Start, DateTime End)>();

            foreach (var busyInterval in Merge(busy))
            {
                if (busyInterval.Start > cursor)
                    gaps.Add((cursor, busyInterval.Start));
                if (busyInterval.End > cursor)
                    cursor = busyInterval.End;
            }

            if (cursor < windowEnd)
                gaps.Add((cursor, windowEnd));

            if (openFrom.HasValue && openUntil.HasValue)
            {
                var working = new List<(DateTime Start, DateTime End)>();
                foreach (var gap in gaps)
                {
                    var day = gap.Start.Date;
                    while (day <= gap.End.Date)
                    {
                        var openStart = day + openFrom.Value;
                        var openEnd = day + openUntil.Value;
                        var start = gap.Start > openStart ? gap.Start : openStart;
                        var end = gap.End < openEnd ? gap.End : openEnd;
                        if (end > start)
                            working.Add((start, end));
                        day = day.AddDays(1).Date;
                    }
                }
                return working;
            }

            return gaps;
        }

        /// <summary>
        /// Đề xuất tối đa count khung giờ độ dài duration nằm trọn trong các khoảng trống,
        /// chọn theo khoảng cách tuyệt đối tới requestedStart (gần nhất trước).
        /// </summary>
        public static IReadOnlyList<(DateTime Start, DateTime End)> SuggestSlots(
            IReadOnlyList<(DateTime Start, DateTime End)> gaps,
            DateTime requestedStart,
            TimeSpan duration,
            int count = 3)
        {
            var candidates = new List<(DateTime Start, DateTime End, double DistanceMinutes)>();
            foreach (var gap in gaps)
            {
                if (gap.End - gap.Start < duration)
                    continue;

                var anchors = new List<DateTime>
                {
                    gap.Start,
                    gap.End - duration,
                    gap.Start + (gap.End - gap.Start - duration) / 2
                };
                anchors.Sort();
                anchors = anchors.Distinct().ToList();

                foreach (var anchor in anchors)
                {
                    var slot = (Start: anchor, End: anchor + duration);
                    if (slot.Start < gap.Start || slot.End > gap.End)
                        continue;

                    var distance = Math.Abs((slot.Start - requestedStart).TotalMinutes);
                    candidates.Add((slot.Start, slot.End, distance));
                }
            }

            return candidates
                .OrderBy(c => c.DistanceMinutes)
                .ThenBy(c => c.Start)
                .DistinctBy(c => (c.Start, c.End))
                .Take(count)
                .Select(c => (c.Start, c.End))
                .ToList();
        }
    }
}