using System.Reflection;

namespace StickyDock.Core.DependencyInjection;

/// <summary>
/// Minimal dependency injection container used by the composition root.
/// </summary>
/// <remarks>
/// It exists to keep the wiring out of the views without taking a third-party dependency that would add
/// startup cost and memory to a background utility (RNF-01, RNF-02). It supports constructor injection,
/// singleton and transient lifetimes, and reports circular dependencies instead of overflowing the stack.
/// </remarks>
public sealed class ServiceContainer : IDisposable
{
    private readonly Dictionary<Type, ServiceDescriptor> _descriptors = [];
    private readonly Dictionary<Type, object> _singletonInstances = [];
    private readonly HashSet<Type> _typesBeingResolved = [];
    private readonly Lock _gate = new();
    private bool _disposed;

    /// <summary>
    /// Registers an already created instance as a singleton.
    /// </summary>
    /// <typeparam name="TService">Service type exposed to consumers.</typeparam>
    /// <param name="instance">Instance to reuse.</param>
    /// <returns>The same container, to allow chaining.</returns>
    public ServiceContainer AddSingleton<TService>(TService instance)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_descriptors.TryAdd(typeof(TService), new ServiceDescriptor(ServiceLifetime.Singleton, _ => instance)))
            {
                throw new InvalidOperationException($"The service '{typeof(TService).FullName}' is already registered.");
            }

            _singletonInstances[typeof(TService)] = instance;
        }

        return this;
    }

    /// <summary>
    /// Registers a singleton created on first resolution by <paramref name="factory"/>.
    /// </summary>
    /// <typeparam name="TService">Service type exposed to consumers.</typeparam>
    /// <param name="factory">Factory invoked once, on first resolution.</param>
    /// <returns>The same container, to allow chaining.</returns>
    public ServiceContainer AddSingleton<TService>(Func<ServiceContainer, TService> factory)
        where TService : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        return Register(typeof(TService), ServiceLifetime.Singleton, container => factory(container));
    }

    /// <summary>
    /// Registers a singleton service implemented by <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">Service type exposed to consumers.</typeparam>
    /// <typeparam name="TImplementation">Concrete type instantiated through constructor injection.</typeparam>
    /// <returns>The same container, to allow chaining.</returns>
    public ServiceContainer AddSingleton<TService, TImplementation>()
        where TImplementation : class, TService
        => Register(typeof(TService), ServiceLifetime.Singleton, container => Activate(container, typeof(TImplementation)));

    /// <summary>
    /// Registers a transient service implemented by <typeparamref name="TImplementation"/>.
    /// </summary>
    /// <typeparam name="TService">Service type exposed to consumers.</typeparam>
    /// <typeparam name="TImplementation">Concrete type instantiated through constructor injection.</typeparam>
    /// <returns>The same container, to allow chaining.</returns>
    public ServiceContainer AddTransient<TService, TImplementation>()
        where TImplementation : class, TService
        => Register(typeof(TService), ServiceLifetime.Transient, container => Activate(container, typeof(TImplementation)));

    /// <summary>
    /// Determines whether a service type has been registered.
    /// </summary>
    /// <param name="serviceType">Service type to look up.</param>
    /// <returns><see langword="true"/> when the type is registered.</returns>
    public bool IsRegistered(Type serviceType)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        lock (_gate)
        {
            return _descriptors.ContainsKey(serviceType);
        }
    }

    /// <summary>
    /// Resolves a registered service or throws when it is not registered.
    /// </summary>
    /// <typeparam name="TService">Service type to resolve.</typeparam>
    /// <returns>The resolved instance.</returns>
    public TService GetRequiredService<TService>()
        where TService : class
        => (TService)ResolveRequired(typeof(TService));

    /// <summary>
    /// Resolves a registered service or returns <see langword="false"/> when it is not registered.
    /// </summary>
    /// <param name="serviceType">Service type to resolve.</param>
    /// <param name="service">Resolved instance, or <see langword="null"/>.</param>
    /// <returns><see langword="true"/> when the service was resolved.</returns>
    public bool TryGetService(Type serviceType, out object? service)
    {
        ArgumentNullException.ThrowIfNull(serviceType);

        lock (_gate)
        {
            if (!_descriptors.ContainsKey(serviceType))
            {
                service = null;
                return false;
            }
        }

        service = ResolveRequired(serviceType);
        return true;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        List<object> instances;

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            _disposed = true;
            instances = [.. _singletonInstances.Values];
            _singletonInstances.Clear();
            _descriptors.Clear();
        }

        foreach (var instance in instances.OfType<IDisposable>())
        {
            instance.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private ServiceContainer Register(Type serviceType, ServiceLifetime lifetime, Func<ServiceContainer, object> factory)
    {
        ArgumentNullException.ThrowIfNull(serviceType);
        ArgumentNullException.ThrowIfNull(factory);

        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (!_descriptors.TryAdd(serviceType, new ServiceDescriptor(lifetime, factory)))
            {
                throw new InvalidOperationException($"The service '{serviceType.FullName}' is already registered.");
            }
        }

        return this;
    }

    private object ResolveRequired(Type serviceType)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        lock (_gate)
        {
            if (_singletonInstances.TryGetValue(serviceType, out var existingInstance))
            {
                return existingInstance;
            }

            if (!_descriptors.TryGetValue(serviceType, out var descriptor))
            {
                throw new InvalidOperationException($"No service is registered for type '{serviceType.FullName}'.");
            }

            if (!_typesBeingResolved.Add(serviceType))
            {
                throw new InvalidOperationException($"A circular dependency was detected while resolving '{serviceType.FullName}'.");
            }

            try
            {
                var instance = descriptor.Factory(this);

                if (descriptor.Lifetime == ServiceLifetime.Singleton)
                {
                    _singletonInstances[serviceType] = instance;
                }

                return instance;
            }
            finally
            {
                _typesBeingResolved.Remove(serviceType);
            }
        }
    }

    private static object Activate(ServiceContainer container, Type implementationType)
    {
        var constructors = implementationType.GetConstructors(BindingFlags.Public | BindingFlags.Instance);

        if (constructors.Length == 0)
        {
            throw new InvalidOperationException($"Type '{implementationType.FullName}' has no public constructor to activate.");
        }

        var selectedParameterCount = constructors.Max(constructor => constructor.GetParameters().Length);
        var candidates = constructors
            .Where(constructor => constructor.GetParameters().Length == selectedParameterCount)
            .ToArray();

        if (candidates.Length > 1)
        {
            throw new InvalidOperationException(
                $"Type '{implementationType.FullName}' has {candidates.Length} public constructors with {selectedParameterCount} parameters; " +
                "the constructor to use is ambiguous.");
        }

        var parameters = candidates[0]
            .GetParameters()
            .Select(parameter => container.ResolveRequired(parameter.ParameterType))
            .ToArray();

        return candidates[0].Invoke(parameters);
    }

    private sealed record ServiceDescriptor(ServiceLifetime Lifetime, Func<ServiceContainer, object> Factory);
}
