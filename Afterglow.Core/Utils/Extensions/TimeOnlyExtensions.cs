using System;

namespace Afterglow.Core.Utils.Extensions;

public static class TimeOnlyExtensions
{
    extension(TimeOnly time)
    {
        public DateTimeOffset NextAfter(DateTimeOffset anchor) =>
            anchor.ToTimeOnly() <= time
                ? anchor.AtTimeOfDay(time)
                : anchor.AddDays(1).AtTimeOfDay(time);

        public DateTimeOffset PreviousBefore(DateTimeOffset anchor) =>
            anchor.ToTimeOnly() > time
                ? anchor.AtTimeOfDay(time)
                : anchor.AddDays(-1).AtTimeOfDay(time);
    }
}
