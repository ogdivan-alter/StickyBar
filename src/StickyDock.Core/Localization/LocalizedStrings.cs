using System.Globalization;
using StickyDock.Core.Docking;
using StickyDock.Core.Mvvm;

namespace StickyDock.Core.Localization;

/// <summary>
/// Provides localized UI strings for Spanish (default) and English, raising <see cref="ObservableObject.PropertyChanged"/>
/// for all bindings when the active language changes.
/// </summary>
public sealed class LocalizedStrings : ViewModelBase
{
    private string _currentLanguage;

    /// <summary>
    /// Initializes a new instance of the <see cref="LocalizedStrings"/> class.
    /// </summary>
    /// <param name="language">Initial language code (<c>"es"</c> or <c>"en"</c>). Defaults to <c>"es"</c>.</param>
    public LocalizedStrings(string? language = SupportedLanguages.Spanish)
    {
        _currentLanguage = SupportedLanguages.Normalize(language);
    }

    /// <summary>Gets the active normalized language code (<c>"es"</c> or <c>"en"</c>).</summary>
    public string CurrentLanguage => _currentLanguage;

    /// <summary>Gets whether the active language is Spanish.</summary>
    public bool IsSpanish => string.Equals(_currentLanguage, SupportedLanguages.Spanish, StringComparison.Ordinal);

    /// <summary>Updates the active language and notifies all bound properties if it changed.</summary>
    public void SetLanguage(string? language)
    {
        var normalized = SupportedLanguages.Normalize(language);
        if (string.Equals(_currentLanguage, normalized, StringComparison.Ordinal))
        {
            return;
        }

        _currentLanguage = normalized;
        OnPropertyChanged(string.Empty);
    }

    /// <summary>Gets the application title.</summary>
    public string AppTitle => "Sticky Dock Utility";

    /// <summary>Gets the tray context menu text for Settings.</summary>
    public string TraySettings => IsSpanish ? "Configuración" : "Settings";

    /// <summary>Gets the tray context menu text for Exit.</summary>
    public string TrayExit => IsSpanish ? "Salir" : "Exit";

    /// <summary>Gets the shortcut context menu header to move an item to the main bar.</summary>
    public string MoveToMainBar => IsSpanish ? "Mover a la barra principal" : "Move to Main Bar";

    /// <summary>Gets the shortcut context menu header to move an item to a ribbon.</summary>
    public string MoveToRibbon => IsSpanish ? "Mover a cinta" : "Move to Ribbon";

    /// <summary>Gets the context menu header / tooltip to move an item or ribbon earlier.</summary>
    public string MoveEarlier => IsSpanish ? "Mover antes" : "Move Earlier";

    /// <summary>Gets the context menu header / tooltip to move an item or ribbon later.</summary>
    public string MoveLater => IsSpanish ? "Mover después" : "Move Later";

    /// <summary>Gets the shortcut context menu header to remove a shortcut.</summary>
    public string Remove => IsSpanish ? "Quitar" : "Remove";

    /// <summary>Gets the ribbon context menu header to delete a ribbon.</summary>
    public string Delete => IsSpanish ? "Eliminar" : "Delete";

    /// <summary>Gets the tooltip for the create ribbon (+) button.</summary>
    public string CreateRibbonToolTip => IsSpanish ? "Crear cinta" : "Create Ribbon";

    /// <summary>Gets the tooltip for the inline ribbon actions button.</summary>
    public string RibbonActionsToolTip => IsSpanish ? "Acciones de cinta" : "Ribbon Actions";

    /// <summary>Gets the tooltip for the inline delete ribbon button.</summary>
    public string DeleteRibbonToolTip => IsSpanish ? "Eliminar cinta" : "Delete Ribbon";

    /// <summary>Gets the alert title when a dropped file is not a <c>.lnk</c> shortcut.</summary>
    public string UnsupportedItemTitle => IsSpanish ? "Elemento no compatible" : "Unsupported Item";

    /// <summary>Gets the alert title when launching a shortcut fails.</summary>
    public string CannotLaunchShortcutTitle => IsSpanish ? "No se puede abrir el acceso directo" : "Cannot Launch Shortcut";

    /// <summary>Gets the settings window title.</summary>
    public string SettingsWindowTitle => IsSpanish ? "Sticky Dock Utility - Configuración" : "Sticky Dock Utility - Settings";

    /// <summary>Gets the Interaction section header in Settings.</summary>
    public string SettingsInteractionHeader => IsSpanish ? "Interacción" : "Interaction";

    /// <summary>Gets the collapse grace period label in Settings.</summary>
    public string SettingsGracePeriodLabel => IsSpanish ? "Tiempo de espera al colapsar" : "Collapse grace period";

    /// <summary>Gets the collapse grace period description in Settings.</summary>
    public string SettingsGracePeriodDescription => IsSpanish
        ? "Retraso antes de colapsar después de que el puntero sale de la barra (300–500 ms)."
        : "Delay before collapsing after the pointer leaves the bar (300–500 ms).";

    /// <summary>Gets the System section header in Settings.</summary>
    public string SettingsSystemHeader => IsSpanish ? "Sistema" : "System";

    /// <summary>Gets the Windows auto-start checkbox label in Settings.</summary>
    public string SettingsAutoStartLabel => IsSpanish ? "Iniciar automáticamente con Windows" : "Start automatically with Windows";

    /// <summary>Gets the current dock position label in Settings.</summary>
    public string SettingsDockPositionLabel => IsSpanish ? "Posición actual de la barra: " : "Current dock position: ";

    /// <summary>Gets the language selector label in Settings.</summary>
    public string SettingsLanguageLabel => IsSpanish ? "Idioma" : "Language";

    /// <summary>Gets the Multimonitor Replica section header in Settings.</summary>
    public string SettingsReplicaHeader => IsSpanish ? "Réplica multimonitor" : "Multimonitor Replica";

    /// <summary>Gets the empty-state message when no secondary monitors are connected.</summary>
    public string SettingsNoSecondaryMonitors => IsSpanish
        ? "Solo se detectó un monitor. Conecta una pantalla secundaria para habilitar barras réplica."
        : "Only one monitor detected. Connect a secondary display to enable mirrored replica bars.";

    /// <summary>Gets the Close button text in Settings.</summary>
    public string SettingsCloseButton => IsSpanish ? "Cerrar" : "Close";

    /// <summary>Formats the warning message when a non-<c>.lnk</c> file is dropped.</summary>
    public string FormatUnsupportedItemMessage(string fileName)
        => IsSpanish
            ? string.Create(
                CultureInfo.CurrentCulture,
                $"'{fileName}' no se puede agregar directamente. Solo se admiten archivos de acceso directo (.lnk); crea primero un acceso directo de Windows (.lnk) para ejecutables, carpetas o scripts.")
            : string.Create(
                CultureInfo.CurrentCulture,
                $"'{fileName}' cannot be added directly. Only .lnk shortcut files are supported; please package executables, folders or scripts as a Windows shortcut (.lnk) first.");

    /// <summary>Formats the fallback error message when launching a shortcut fails.</summary>
    public string FormatCannotLaunchShortcutMessage(string name)
        => IsSpanish
            ? string.Create(CultureInfo.CurrentCulture, $"No se pudo abrir '{name}'.")
            : string.Create(CultureInfo.CurrentCulture, $"Could not launch '{name}'.");

    /// <summary>Formats the confirmation message for deleting a non-empty ribbon.</summary>
    public string FormatConfirmDeleteRibbon(string ribbonName, int itemCount)
        => IsSpanish
            ? string.Create(
                CultureInfo.CurrentCulture,
                $"¿Eliminar la cinta \"{ribbonName}\" y sus {itemCount} acceso(s) directo(s)? Esta acción no se puede deshacer.")
            : string.Create(
                CultureInfo.CurrentCulture,
                $"Delete ribbon \"{ribbonName}\" and its {itemCount} shortcut(s)? This action cannot be undone.");

    /// <summary>Formats a <see cref="DockEdge"/> (or floating <see langword="null"/>) for display.</summary>
    public string FormatDockEdge(DockEdge? edge)
        => edge switch
        {
            DockEdge.Top => IsSpanish ? "Superior" : "Top",
            DockEdge.Left => IsSpanish ? "Izquierda" : "Left",
            DockEdge.Right => IsSpanish ? "Derecha" : "Right",
            DockEdge.Bottom => IsSpanish ? "Inferior" : "Bottom",
            null => IsSpanish ? "Flotante" : "Floating",
            _ => edge.ToString() ?? string.Empty,
        };

    /// <summary>Formats a secondary display label in Settings.</summary>
    public string FormatSecondaryMonitor(int index, string deviceName, int width, int height)
        => IsSpanish
            ? string.Create(CultureInfo.InvariantCulture, $"Pantalla secundaria {index} ({deviceName}, {width}×{height})")
            : string.Create(CultureInfo.InvariantCulture, $"Secondary Display {index} ({deviceName}, {width}×{height})");
}
