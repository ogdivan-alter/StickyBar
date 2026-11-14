using Microsoft.Win32;
using StickyDock.Core.Services;

namespace StickyDock.Interop.Win32;

/// <summary>
/// Manages the per-user Windows Run registry entry for automatic startup (§4.6-03, E6-CA4).
/// </summary>
public sealed class WindowsAutoStartService : IAutoStartService
{
    /// <summary>
    /// Standard per-user registry subkey under <c>HKEY_CURRENT_USER</c> for startup applications.
    /// </summary>
    public const string DefaultRegistrySubKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>
    /// Default registry value name under the Run subkey.
    /// </summary>
    public const string DefaultValueName = "StickyDock";

    private readonly string _registrySubKeyPath;
    private readonly string _valueName;
    private readonly Func<string?> _executablePathProvider;

    /// <summary>
    /// Initializes a new instance of the <see cref="WindowsAutoStartService"/> class.
    /// </summary>
    /// <param name="registrySubKeyPath">Subkey path under <see cref="Registry.CurrentUser"/>.</param>
    /// <param name="valueName">Registry value name.</param>
    /// <param name="executablePathProvider">Provider for the executable path; defaults to <see cref="Environment.ProcessPath"/>.</param>
    public WindowsAutoStartService(
        string registrySubKeyPath = DefaultRegistrySubKeyPath,
        string valueName = DefaultValueName,
        Func<string?>? executablePathProvider = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(registrySubKeyPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(valueName);

        _registrySubKeyPath = registrySubKeyPath;
        _valueName = valueName;
        _executablePathProvider = executablePathProvider ?? (() => Environment.ProcessPath);
    }

    /// <inheritdoc />
    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(_registrySubKeyPath, writable: false);

        if (key is null)
        {
            return false;
        }

        return key.GetValue(_valueName) is string value && !string.IsNullOrWhiteSpace(value);
    }

    /// <inheritdoc />
    public void SetEnabled(bool enabled)
    {
        if (enabled)
        {
            var executablePath = _executablePathProvider();

            if (string.IsNullOrWhiteSpace(executablePath))
            {
                throw new InvalidOperationException("The current executable path could not be resolved.");
            }

            using var key = Registry.CurrentUser.CreateSubKey(_registrySubKeyPath, writable: true)
                ?? throw new InvalidOperationException($"Could not open registry subkey '{_registrySubKeyPath}'.");

            key.SetValue(_valueName, $"\"{executablePath}\"", RegistryValueKind.String);
            return;
        }

        using var existingKey = Registry.CurrentUser.OpenSubKey(_registrySubKeyPath, writable: true);
        existingKey?.DeleteValue(_valueName, throwOnMissingValue: false);
    }
}
