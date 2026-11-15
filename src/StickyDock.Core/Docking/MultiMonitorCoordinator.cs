using StickyDock.Core.Configuration;
using StickyDock.Core.Diagnostics;

namespace StickyDock.Core.Docking;

/// <summary>
/// Coordinates mirrored replica bars across secondary monitors, reacting to topology changes and configuration updates
/// (E7-CA4, E7-CA5).
/// </summary>
public sealed class MultiMonitorCoordinator : IDisposable
{
    private readonly IMonitorCatalog _monitorCatalog;
    private readonly IConfigurationDocument _configuration;
    private readonly IBarHostFactory _hostFactory;
    private readonly ILogger _logger;
    private readonly Dictionary<string, IBarHost> _activeReplicas = new(StringComparer.OrdinalIgnoreCase);

    private bool _started;
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiMonitorCoordinator"/> class.
    /// </summary>
    public MultiMonitorCoordinator(
        IMonitorCatalog monitorCatalog,
        IConfigurationDocument configuration,
        IBarHostFactory hostFactory,
        ILogger logger)
    {
        _monitorCatalog = monitorCatalog ?? throw new ArgumentNullException(nameof(monitorCatalog));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _hostFactory = hostFactory ?? throw new ArgumentNullException(nameof(hostFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Gets the device names of the secondary monitors that currently have an active replica bar.
    /// </summary>
    public IReadOnlyCollection<string> ActiveReplicaDeviceNames => _activeReplicas.Keys;

    /// <summary>
    /// Starts reconciling active replica bars and subscribes to configuration changes.
    /// </summary>
    public void Start()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_started)
        {
            return;
        }

        _started = true;
        _configuration.Changed += OnConfigurationChanged;
        Reconcile();
    }

    /// <summary>
    /// Re-evaluates the connected monitors and opens or closes replica bars accordingly (E7-CA4, E7-CA5).
    /// </summary>
    public void NotifyDisplayChanged()
    {
        if (_disposed || !_started)
        {
            return;
        }

        Reconcile();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _configuration.Changed -= OnConfigurationChanged;

        foreach (var host in _activeReplicas.Values)
        {
            DisposeHostSafely(host);
        }

        _activeReplicas.Clear();
    }

    private void OnConfigurationChanged(object? sender, ConfigurationChangedEventArgs e)
    {
        if (_disposed || !_started)
        {
            return;
        }

        Reconcile();
    }

    private void Reconcile()
    {
        var enabledMonitors = new HashSet<string>(
            _configuration.Current.EnabledReplicaMonitors ?? [],
            StringComparer.OrdinalIgnoreCase);

        var secondaryMonitors = _monitorCatalog
            .GetMonitors()
            .Where(static monitor => !monitor.IsPrimary && !string.IsNullOrWhiteSpace(monitor.DeviceName))
            .ToDictionary(static monitor => monitor.DeviceName, StringComparer.OrdinalIgnoreCase);

        var desired = new Dictionary<string, DisplayMonitorInfo>(StringComparer.OrdinalIgnoreCase);

        foreach (var pair in secondaryMonitors)
        {
            if (enabledMonitors.Contains(pair.Key))
            {
                desired[pair.Key] = pair.Value;
            }
        }

        // Close replicas whose monitor was disconnected or disabled (E7-CA4).
        var toRemove = _activeReplicas.Keys
            .Where(deviceName => !desired.ContainsKey(deviceName))
            .ToArray();

        foreach (var deviceName in toRemove)
        {
            if (_activeReplicas.Remove(deviceName, out var host))
            {
                DisposeHostSafely(host);
                _logger.Information($"Closed replica dock bar on monitor '{deviceName}'.");
            }
        }

        // Open replicas for newly connected or newly enabled monitors (E7-CA5).
        foreach (var pair in desired)
        {
            if (_activeReplicas.ContainsKey(pair.Key))
            {
                continue;
            }

            try
            {
                var host = _hostFactory.CreateReplica(pair.Value);
                _activeReplicas[pair.Key] = host;
                _logger.Information($"Opened replica dock bar on monitor '{pair.Key}'.");
            }
            catch (Exception exception)
            {
                _logger.Error($"Could not create replica dock bar for monitor '{pair.Key}'.", exception);
            }
        }
    }

    private void DisposeHostSafely(IBarHost host)
    {
        try
        {
            host.Dispose();
        }
        catch (Exception exception)
        {
            _logger.Error($"Error while disposing replica dock bar on monitor '{host.DeviceName}'.", exception);
        }
    }
}
