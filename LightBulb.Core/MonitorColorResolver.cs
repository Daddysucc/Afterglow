namespace LightBulb.Core;

/// <summary>
/// Everything (other than the display itself) that determines a display's color configuration.
/// </summary>
/// <param name="ActiveConfiguration">Configuration of the cycle, used while LightBulb is active.</param>
/// <param name="PausedConfiguration">Configuration used while paused (neutral, or day-time if configured so).</param>
/// <param name="IsGloballyPaused">Whether all displays are paused (disabled, streaming, locked, etc).</param>
public readonly record struct MonitorColorContext(
    ColorConfiguration ActiveConfiguration,
    ColorConfiguration PausedConfiguration,
    bool IsGloballyPaused,
    double MinimumTemperature,
    double MaximumTemperature,
    double MinimumBrightness,
    double MaximumBrightness
);

public static class MonitorColorResolver
{
    // Neutral configuration that excluded displays are kept at
    public static ColorConfiguration NeutralConfiguration => ColorConfiguration.Default;

    /// <summary>
    /// Resolves the color configuration of a single display.
    /// </summary>
    /// <param name="context">State shared by all displays.</param>
    /// <param name="profile">Preferences of this display, if any.</param>
    /// <param name="isPaused">Whether this display alone is paused (e.g. it's running a fullscreen application).</param>
    /// <param name="isPrimary">
    /// Whether this is the primary display, which serves as the reference that other displays are
    /// calibrated against. It always follows the cycle exactly: its offsets are ignored (but kept,
    /// in case a different display becomes the primary one later).
    /// </param>
    public static ColorConfiguration Resolve(
        MonitorColorContext context,
        MonitorProfile? profile,
        bool isPaused = false,
        bool isPrimary = false
    )
    {
        // Excluded displays always stay neutral, regardless of anything else
        if (profile is { IsEnabled: false })
            return NeutralConfiguration;

        if (context.IsGloballyPaused || isPaused)
            return context.PausedConfiguration;

        if (profile is null || isPrimary)
            return context.ActiveConfiguration;

        return context
            .ActiveConfiguration.WithOffset(
                MonitorProfile.NormalizeTemperatureOffset(profile.TemperatureOffset),
                MonitorProfile.NormalizeBrightnessOffset(profile.BrightnessOffset)
            )
            .Clamp(
                context.MinimumTemperature,
                context.MaximumTemperature,
                context.MinimumBrightness,
                context.MaximumBrightness
            );
    }
}
