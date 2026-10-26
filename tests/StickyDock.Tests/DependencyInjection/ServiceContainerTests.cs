using StickyDock.Core.DependencyInjection;

namespace StickyDock.Tests.DependencyInjection;

/// <summary>
/// Behaviour of the composition-root container: lifetimes, constructor injection and failure modes.
/// </summary>
public sealed class ServiceContainerTests
{
    [Fact]
    public void GetRequiredService_WhenTheServiceIsASingleton_ReturnsTheSameInstance()
    {
        using var container = new ServiceContainer()
            .AddSingleton<IClock, SystemClock>();

        var first = container.GetRequiredService<IClock>();
        var second = container.GetRequiredService<IClock>();

        Assert.Same(first, second);
    }

    [Fact]
    public void GetRequiredService_WhenTheServiceIsTransient_ReturnsANewInstance()
    {
        using var container = new ServiceContainer()
            .AddTransient<IClock, SystemClock>();

        var first = container.GetRequiredService<IClock>();
        var second = container.GetRequiredService<IClock>();

        Assert.NotSame(first, second);
    }

    [Fact]
    public void GetRequiredService_WhenTheServiceIsAnInstance_ReturnsThatInstance()
    {
        var clock = new SystemClock();
        using var container = new ServiceContainer().AddSingleton<IClock>(clock);

        Assert.Same(clock, container.GetRequiredService<IClock>());
    }

    [Fact]
    public void GetRequiredService_ResolvesConstructorDependencies()
    {
        using var container = new ServiceContainer()
            .AddSingleton<IClock, SystemClock>()
            .AddSingleton<ICalendar, Calendar>();

        var calendar = container.GetRequiredService<ICalendar>();

        Assert.NotNull(calendar.Clock);
    }

    [Fact]
    public void GetRequiredService_WhenTheServiceIsNotRegistered_Throws()
    {
        using var container = new ServiceContainer();

        Assert.Throws<InvalidOperationException>(() => container.GetRequiredService<ICalendar>());
    }

    [Fact]
    public void GetRequiredService_WhenTheDependencyIsNotRegistered_Throws()
    {
        using var container = new ServiceContainer().AddSingleton<ICalendar, Calendar>();

        Assert.Throws<InvalidOperationException>(() => container.GetRequiredService<ICalendar>());
    }

    [Fact]
    public void GetRequiredService_WhenTheDependencyIsCircular_Throws()
    {
        using var container = new ServiceContainer()
            .AddSingleton<ICircularFirst, CircularFirst>()
            .AddSingleton<ICircularSecond, CircularSecond>();

        var exception = Assert.Throws<InvalidOperationException>(() => container.GetRequiredService<ICircularFirst>());

        Assert.Contains("circular", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Register_WhenTheTypeIsAlreadyRegistered_Throws()
    {
        using var container = new ServiceContainer().AddSingleton<IClock, SystemClock>();

        Assert.Throws<InvalidOperationException>(() => container.AddSingleton<IClock, SystemClock>());
    }

    [Fact]
    public void TryGetService_ReflectsTheRegistrations()
    {
        using var container = new ServiceContainer().AddSingleton<IClock, SystemClock>();

        Assert.True(container.TryGetService(typeof(IClock), out var service));
        Assert.IsType<SystemClock>(service);
        Assert.False(container.TryGetService(typeof(ICalendar), out var missing));
        Assert.Null(missing);
        Assert.True(container.IsRegistered(typeof(IClock)));
        Assert.False(container.IsRegistered(typeof(ICalendar)));
    }

    [Fact]
    public void Dispose_DisposesTheSingletonsItCreated()
    {
        var disposable = new DisposableClock();
        var container = new ServiceContainer().AddSingleton<IClock>(disposable);

        container.Dispose();

        Assert.True(disposable.IsDisposed);
    }

    [Fact]
    public void GetRequiredService_AfterDispose_Throws()
    {
        var container = new ServiceContainer().AddSingleton<IClock, SystemClock>();
        container.Dispose();

        Assert.Throws<ObjectDisposedException>(() => container.GetRequiredService<IClock>());
    }

    private interface IClock
    {
    }

    private interface ICalendar
    {
        IClock Clock { get; }
    }

    private interface ICircularFirst
    {
    }

    private interface ICircularSecond
    {
    }

    private sealed class SystemClock : IClock
    {
    }

    private sealed class Calendar : ICalendar
    {
        public Calendar(IClock clock) => Clock = clock;

        public IClock Clock { get; }
    }

    private sealed class CircularFirst : ICircularFirst
    {
        public CircularFirst(ICircularSecond second) => Second = second;

        public ICircularSecond Second { get; }
    }

    private sealed class CircularSecond : ICircularSecond
    {
        public CircularSecond(ICircularFirst first) => First = first;

        public ICircularFirst First { get; }
    }

    private sealed class DisposableClock : IClock, IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
