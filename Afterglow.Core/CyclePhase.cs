namespace Afterglow.Core;

/// <summary>
/// Position within the daily cycle.
/// </summary>
/// <param name="Stage">
/// Stage that is currently active or, if <paramref name="IsTransitioning" /> is set,
/// the stage that the cycle is currently transitioning into.
/// </param>
/// <param name="IsTransitioning">Whether a transition is currently in progress.</param>
public readonly record struct CyclePhase(CycleStage Stage, bool IsTransitioning);
