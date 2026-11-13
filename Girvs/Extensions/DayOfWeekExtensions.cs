namespace Girvs.Extensions;

/// <summary>
/// Extension methods for <see cref="DayOfWeekExtensions"/>.
/// </summary>
public static class DayOfWeekExtensions
{
    /// <summary>
    /// Check if a given <see cref="DayOfWeek"/> value is weekend.
    /// </summary>
    public static bool IsWeekend(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek.IsIn(DayOfWeek.Saturday, DayOfWeek.Sunday);
    }

    /// <summary>
    /// Check if a given <see cref="DayOfWeek"/> value is weekday.
    /// </summary>
    public static bool IsWeekday(this DayOfWeek dayOfWeek)
    {
        return dayOfWeek.IsIn(DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday);
    }

    /// <summary>
    /// Finds the NTH week day of a month.
    /// </summary>
    /// <param name="dayOfWeek">The day of week.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <param name="n">The nth instance.</param>
    /// <remarks>Compensates for 4th and 5th DayOfWeek of Month</remarks>
    public static DateTime FindNthWeekDayOfMonth(this DayOfWeek dayOfWeek, int year, int month, int n)
    {
        if (n < 1 || n > 5)
        {
            throw new ArgumentOutOfRangeException(nameof(n));
        }

        // compensate for "last DayOfWeek in month"
        var totalInstances = dayOfWeek.TotalInstancesInMonth(year, month);
        if (n == 5 && n > totalInstances)
            n = 4;

        // 第一个匹配日 + 7 天偏移即为第 n 个，直接构造日期。
        var offset = GetFirstOccurrenceOffset(dayOfWeek, year, month);
        var day = offset + 1 + (n - 1) * 7;

        return new DateTime(year, month, day);
    }

    /// <summary>
    /// Finds the total number of instances of a specific DayOfWeek in a month.
    /// </summary>
    /// <param name="dayOfWeek">The day of week.</param>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns></returns>
    public static int TotalInstancesInMonth(this DayOfWeek dayOfWeek, int year, int month)
    {
        // O(1) 计算：从当月 1 日到第一个匹配日之间的偏移量决定总出现次数。
        var offset = GetFirstOccurrenceOffset(dayOfWeek, year, month);
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return (daysInMonth - 1 - offset) / 7 + 1;
    }

    /// <summary>
    /// 当月第一天到第一个 <paramref name="dayOfWeek"/> 的偏移天数（0-6）。
    /// </summary>
    private static int GetFirstOccurrenceOffset(DayOfWeek dayOfWeek, int year, int month)
    {
        var firstDayOfWeek = new DateTime(year, month, 1).DayOfWeek;
        return ((int)dayOfWeek - (int)firstDayOfWeek + 7) % 7;
    }

    /// <summary>
    /// Gets the total number of instances of a specific DayOfWeek in a month.
    /// </summary>
    /// <param name="dayOfWeek">The day of week.</param>
    /// <param name="dateTime">The date in a month.</param>
    /// <returns></returns>
    public static int TotalInstancesInMonth(this DayOfWeek dayOfWeek, DateTime dateTime)
    {
        return dayOfWeek.TotalInstancesInMonth(dateTime.Year, dateTime.Month);
    }
}