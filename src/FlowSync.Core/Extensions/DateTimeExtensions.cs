namespace FlowSync.Core.Extensions;

internal static class DateTimeExtensions
{
    public static DateTime AddYears(this DateTime dateTime, double value)
    {
        var roundedValue = (int)Math.Floor(value);
        var partialValue = value - roundedValue;
        var result = dateTime.AddYears(roundedValue);
        return result.AddMonths(Math.Floor(result.AddYears(1).Subtract(result).TotalDays * partialValue));
    }

    public static DateTime AddMonths(this DateTime dateTime, double value)
    {
        var roundedValue = (int)Math.Floor(value);
        var partialValue = value - roundedValue;
        var result = dateTime.AddMonths(roundedValue);
        return result.AddDays(Math.Floor(result.AddMonths(1).Subtract(result).TotalDays * partialValue));
    }

    public static DateTime AddWeeks(this DateTime dateTime, double value)
    {
        var roundedValue = (int)Math.Floor(value);
        var partialValue = value - roundedValue;
        var result = dateTime.AddDays(roundedValue * 7);
        return result.AddDays(Math.Floor(result.AddDays(7).Subtract(result).TotalDays * (partialValue * 7)));
    }
}