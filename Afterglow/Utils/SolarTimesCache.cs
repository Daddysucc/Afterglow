using System;
using Afterglow.Core;

namespace Afterglow.Utils;

/// <summary>
/// Remembers the last calculated sunrise and sunset times, which only depend on the location,
/// the day of the year, and the UTC offset, so that they're not recalculated on every update.
/// </summary>
public class SolarTimesCache
{
    private (GeoLocation Location, int DayOfYear, TimeSpan Offset)? _key;
    private SolarTimes _solarTimes;

    public SolarTimes Get(GeoLocation location, DateTimeOffset instant)
    {
        var key = (location, instant.DayOfYear, instant.Offset);
        if (_key != key)
        {
            _solarTimes = SolarTimes.Calculate(location, instant);
            _key = key;
        }

        return _solarTimes;
    }
}
