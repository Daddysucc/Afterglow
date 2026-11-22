using System;

namespace Afterglow.Framework;

/// <summary>
/// An object created in its own dependency injection scope. Disposing it disposes everything that
/// was created for it (e.g. transient view models, which unsubscribes them from long-lived
/// services), and lets it all be garbage collected. Transient objects created from the
/// application's root container are instead kept by it until the application exits.
/// </summary>
public sealed class Owned<T>(T value, IDisposable scope) : IDisposable
    where T : notnull
{
    public T Value { get; } = value;

    public void Dispose() => scope.Dispose();
}
