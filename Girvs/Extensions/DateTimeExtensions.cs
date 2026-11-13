namespace Girvs.Extensions;

/// <summary>
/// Extension methods for <see cref="DateTime"/>.
/// </summary>
public static class DateTimeExtensions
{
    /// <summary>
    /// Converts a DateTime to a Unix Timestamp
    /// </summary>
    /// <param name="target">This DateTime</param>
    /// <returns></returns>
    public static double ToUnixTimestamp(this DateTime target)
    {
        var origin = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        var diff = target - origin;
        return Math.Floor(diff.TotalSeconds);
    }

    /// <summary>
    /// Converts a Unix Timestamp in to a DateTime
    /// </summary>
    /// <param name="unixTime">This Unix Timestamp</param>
    /// <returns></returns>
    public static DateTime FromUnixTimestamp(this double unixTime)
    {
        var epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
        return epoch.AddSeconds(unixTime);
    }

    /// <summary>
    /// Gets the value of the End of the day (23:59)
    /// </summary>
    /// <param name="target"></param>
    /// <returns></returns>
    public static DateTime ToDayEnd(this DateTime target)
    {
        return target.Date.AddDays(1).AddMilliseconds(-1);
    }

    /// <summary>
    /// Gets the First Date of the week for the specified date
    /// </summary>
    /// <param name="dt">this DateTime</param>
    /// <param name="startOfWeek">The Start Day of the Week (ie, Sunday/Monday)</param>
    /// <returns>The First Date of the week</returns>
    public static DateTime StartOfWeek(this DateTime dt, DayOfWeek startOfWeek)
    {
        var diff = dt.DayOfWeek - startOfWeek;

        if (diff < 0)
            diff += 7;

        return dt.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Returns all the days of a month.
    /// </summary>
    /// <param name="year">The year.</param>
    /// <param name="month">The month.</param>
    /// <returns></returns>
    public static IEnumerable<DateTime> DaysOfMonth(int year, int month)
    {
        return Enumerable.Range(0, DateTime.DaysInMonth(year, month))
            .Select(day => new DateTime(year, month, day + 1));
    }

    /// <summary>
    /// Determines the Nth instance of a Date's DayOfWeek in a month
    /// </summary>
    /// <returns></returns>
    /// <example>11/29/2011 would return 5, because it is the 5th Tuesday of each month</example>
    public static int WeekDayInstanceOfMonth(this DateTime dateTime)
    {
        // 同一个月内同一星期几每隔 7 天出现一次，可直接由日期算出序号。
        return ((dateTime.Day - 1) / 7) + 1;
    }

    /// <summary>
    /// Gets the total days in a month
    /// </summary>
    /// <param name="dateTime">The date time.</param>
    /// <returns></returns>
    public static int TotalDaysInMonth(this DateTime dateTime)
    {
        // 直接查日历，无需枚举当月每一天。
        return DateTime.DaysInMonth(dateTime.Year, dateTime.Month);
    }
        
    /// <summary>
    /// Takes any date and returns it's value as an Unspecified DateTime
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime ToDateTimeUnspecified(this DateTime date)
    {
        if (date.Kind == DateTimeKind.Unspecified)
        {
            return date;
        }

        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, DateTimeKind.Unspecified);
    }

    /// <summary>
    /// Trims the milliseconds off of a datetime
    /// </summary>
    /// <param name="date"></param>
    /// <returns></returns>
    public static DateTime TrimMilliseconds(this DateTime date)
    {
        return new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Kind);
    }
}