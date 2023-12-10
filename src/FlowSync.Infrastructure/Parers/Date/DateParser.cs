using EnsureThat;
using FlowSync.Core.Common.Services;
using FlowSync.Core.Parers.Date;
using FlowSync.Infrastructure.Exceptions;
using FlowSync.Infrastructure.Extensions;
using Microsoft.Extensions.Logging;

namespace FlowSync.Infrastructure.Parers.Date;

internal class DateParser : IDateParser
{
    public double Years { get; private set; } = 0.0;
    public double Months { get; private set; } = 0.0;
    public double Weeks { get; private set; } = 0.0;
    public double Days { get; private set; } = 0.0;
    public double Hours { get; private set; } = 0.0;
    public double Minutes { get; private set; } = 0.0;
    public double Seconds { get; private set; } = 0.0;
    public double Milliseconds { get; private set; } = 0.0;
    private readonly ILogger<DateParser> _logger;
    private readonly ISystemClock _systemClock;

    public DateParser(ILogger<DateParser> logger, ISystemClock systemClock)
    {
        EnsureArg.IsNotNull(logger, nameof(logger));
        EnsureArg.IsNotNull(systemClock, nameof(systemClock));
        _logger = logger;
        _systemClock = systemClock;
    }

    public DateTime Parse(string dateTime)
    {
        var isDateTime = DateTime.TryParse(dateTime, out var dateTimeResult);
        if (isDateTime)
            return dateTimeResult;

        var isDateTimeDouble = double.TryParse(dateTime, out var doubleResult);
        if (isDateTimeDouble)
            dateTime += 's';

        return ParseDateTimeWithSuffix(dateTime);
    }

    protected DateTime ParseDateTimeWithSuffix(string dateTime)
    {
        if (!HasSuffix(dateTime))
        {
            _logger.LogError($"The given datetime '{dateTime}' is not valid!");
            throw new DateParserException(FlowSyncInfrastructureResource.DateParserInvalidInput);
        }

        var lastPos = 0;
        var phase = DateTimeSpanPhase.Years;

        while (phase != DateTimeSpanPhase.Done)
        {
            switch (phase)
            {
                case DateTimeSpanPhase.Years:
                    Years = ExtractValue(dateTime, "y", ref lastPos);
                    phase = DateTimeSpanPhase.Months;
                    break;
                case DateTimeSpanPhase.Months:
                    Months = ExtractValue(dateTime, "M", ref lastPos);
                    phase = DateTimeSpanPhase.Weeks;
                    break;
                case DateTimeSpanPhase.Weeks:
                    Weeks = ExtractValue(dateTime, "w", ref lastPos);
                    phase = DateTimeSpanPhase.Days;
                    break;
                case DateTimeSpanPhase.Days:
                    Days = ExtractValue(dateTime, "d", ref lastPos);
                    phase = DateTimeSpanPhase.Hours;
                    break;
                case DateTimeSpanPhase.Hours:
                    Hours = ExtractValue(dateTime, "h", ref lastPos);
                    phase = DateTimeSpanPhase.Minutes;
                    break;
                case DateTimeSpanPhase.Minutes:
                    if (dateTime.IndexOf("ms", StringComparison.Ordinal) < 0)
                        Minutes = ExtractValue(dateTime, "m", ref lastPos);

                    phase = DateTimeSpanPhase.Seconds;
                    break;
                case DateTimeSpanPhase.Seconds:
                    if (dateTime.IndexOf("ms", StringComparison.Ordinal) < 0)
                        Seconds = ExtractValue(dateTime, "s", ref lastPos);

                    phase = DateTimeSpanPhase.Milliseconds;
                    break;
                case DateTimeSpanPhase.Milliseconds:
                    Milliseconds = ExtractValue(dateTime, "ms", ref lastPos);
                    phase = DateTimeSpanPhase.Done;
                    break;
            }
        }

        return _systemClock.NowUtc
            .AddYears(Years).AddMonths(Months)
            .AddWeeks(Weeks).AddDays(Days)
            .AddHours(Hours).AddMinutes(Minutes)
            .AddSeconds(Seconds).AddMilliseconds(Milliseconds);
    }

    protected bool HasSuffix(string date)
    {
        return date.Contains("y") || date.Contains("M") ||
               date.Contains("w") || date.Contains("d") ||
               date.Contains("h") || date.Contains("m") ||
               date.Contains("s") || date.Contains("ms");
    }

    protected double ExtractValue(string dateTime, string key, ref int position)
    {
        var charLocation = dateTime.IndexOf(key, StringComparison.Ordinal);
        if (charLocation < 0)
        {
            _logger.LogWarning($"The value from ({dateTime}) could not be extracted!");
            throw new DateParserException(string.Format(FlowSyncInfrastructureResource.DateParserCannotExtractValue, dateTime));
        }

        var extractedValue = dateTime.Substring(position, charLocation - position);
        var validValue = double.TryParse(extractedValue, out var val);
        position = charLocation + 1;
        return validValue ? val : 0.0;
    }

    public void Dispose() { }
}