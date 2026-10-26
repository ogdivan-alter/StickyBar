namespace StickyDock.Core.DependencyInjection;

/// <summary>
/// Lifetime of a service registration.
/// </summary>
public enum ServiceLifetime
{
    /// <summary>A single instance is shared by every resolution.</summary>
    Singleton,

    /// <summary>A new instance is created for every resolution.</summary>
    Transient,
}
