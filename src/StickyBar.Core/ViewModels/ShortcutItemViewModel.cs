using System.Windows.Input;
using StickyBar.Core.Mvvm;
using StickyBar.Core.Ribbons;

namespace StickyBar.Core.ViewModels;

/// <summary>
/// View model for a single <c>.lnk</c> shortcut entry on the main bar or inside a ribbon (E5).
/// </summary>
public sealed class ShortcutItemViewModel : ViewModelBase
{
    private readonly Func<IReadOnlyList<RibbonViewModel>> _getRibbons;
    private readonly Action<ShortcutItemViewModel> _launch;
    private readonly Action<string> _remove;
    private readonly Action<string> _moveToBar;
    private readonly Func<string, string, bool> _moveToRibbon;
    private readonly Action<string> _moveEarlier;
    private readonly Action<string> _moveLater;

    private string _name;
    private int _order;
    private string? _containerRibbonId;
    private byte[]? _iconPngBytes;
    private string _targetPath;

    /// <summary>
    /// Initializes a new instance of the <see cref="ShortcutItemViewModel"/> class.
    /// </summary>
    public ShortcutItemViewModel(
        RibbonItem model,
        string? containerRibbonId,
        byte[]? iconPngBytes,
        string targetPath,
        Func<IReadOnlyList<RibbonViewModel>> getRibbons,
        Action<ShortcutItemViewModel> launch,
        Action<string> remove,
        Action<string> moveToBar,
        Func<string, string, bool> moveToRibbon,
        Action<string> moveEarlier,
        Action<string> moveLater)
    {
        ArgumentNullException.ThrowIfNull(model);

        Id = model.Id;
        _name = model.Name;
        ShortcutPath = model.ShortcutPath;
        _order = model.Order;
        _containerRibbonId = containerRibbonId;
        _iconPngBytes = iconPngBytes;
        _targetPath = targetPath ?? string.Empty;

        _getRibbons = getRibbons ?? throw new ArgumentNullException(nameof(getRibbons));
        _launch = launch ?? throw new ArgumentNullException(nameof(launch));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        _moveToBar = moveToBar ?? throw new ArgumentNullException(nameof(moveToBar));
        _moveToRibbon = moveToRibbon ?? throw new ArgumentNullException(nameof(moveToRibbon));
        _moveEarlier = moveEarlier ?? throw new ArgumentNullException(nameof(moveEarlier));
        _moveLater = moveLater ?? throw new ArgumentNullException(nameof(moveLater));

        LaunchCommand = new RelayCommand(() => _launch(this));
        RemoveCommand = new RelayCommand(() => _remove(Id));
        MoveToBarCommand = new RelayCommand(() => _moveToBar(Id), () => IsInRibbon);
        MoveToRibbonCommand = new RelayCommand<string>(
            targetRibbonId =>
            {
                if (!string.IsNullOrWhiteSpace(targetRibbonId))
                {
                    _moveToRibbon(Id, targetRibbonId);
                }
            },
            targetRibbonId => !string.IsNullOrWhiteSpace(targetRibbonId)
                && !string.Equals(targetRibbonId, ContainerRibbonId, StringComparison.Ordinal));
        MoveEarlierCommand = new RelayCommand(() => _moveEarlier(Id));
        MoveLaterCommand = new RelayCommand(() => _moveLater(Id));
    }

    /// <summary>Gets the unique identifier of this shortcut entry.</summary>
    public string Id { get; }

    /// <summary>Gets the display name of the shortcut.</summary>
    public string Name
    {
        get => _name;
        internal set => SetProperty(ref _name, value);
    }

    /// <summary>Gets the path to the underlying <c>.lnk</c> file (read-only, Q-06).</summary>
    public string ShortcutPath { get; }

    /// <summary>Gets the zero-based position of this item inside its container.</summary>
    public int Order
    {
        get => _order;
        internal set => SetProperty(ref _order, value);
    }

    /// <summary>Gets the identifier of the containing ribbon, or <see langword="null"/> when on the main bar.</summary>
    public string? ContainerRibbonId
    {
        get => _containerRibbonId;
        internal set
        {
            if (SetProperty(ref _containerRibbonId, value))
            {
                OnPropertyChanged(nameof(IsInRibbon));
            }
        }
    }

    /// <summary>Gets whether this item is currently inside a ribbon.</summary>
    public bool IsInRibbon => _containerRibbonId is not null;

    /// <summary>Gets the optional PNG-encoded icon bytes extracted from the <c>.lnk</c>.</summary>
    public byte[]? IconPngBytes
    {
        get => _iconPngBytes;
        internal set => SetProperty(ref _iconPngBytes, value);
    }

    /// <summary>Gets the resolved target path of the <c>.lnk</c> (for tooltip display, read-only).</summary>
    public string TargetPath
    {
        get => _targetPath;
        internal set
        {
            if (SetProperty(ref _targetPath, value))
            {
                OnPropertyChanged(nameof(ToolTipText));
            }
        }
    }

    /// <summary>Gets the tooltip text shown for this shortcut.</summary>
    public string ToolTipText => string.IsNullOrWhiteSpace(_targetPath)
        ? _name
        : $"{_name}\n{_targetPath}";

    /// <summary>Gets the dynamic list of ribbons available for the "Move to Ribbon" context submenu (E5-CA4).</summary>
    public IReadOnlyList<RibbonViewModel> AllRibbons => _getRibbons();

    /// <summary>Gets the command that launches the shortcut preserving its <c>.lnk</c> arguments (E5-CA2).</summary>
    public ICommand LaunchCommand { get; }

    /// <summary>Gets the command that removes this shortcut from its container (E5-CA4).</summary>
    public ICommand RemoveCommand { get; }

    /// <summary>Gets the command that moves this shortcut back to the main bar (E5-CA4).</summary>
    public ICommand MoveToBarCommand { get; }

    /// <summary>Gets the command that moves this shortcut to the ribbon with the given identifier (E5-CA4).</summary>
    public ICommand MoveToRibbonCommand { get; }

    /// <summary>Gets the command that moves this shortcut one slot earlier in its container (E5-CA5).</summary>
    public ICommand MoveEarlierCommand { get; }

    /// <summary>Gets the command that moves this shortcut one slot later in its container (E5-CA5).</summary>
    public ICommand MoveLaterCommand { get; }

    internal RibbonItem ToModel() => new(Id, Name, ShortcutPath, Order);
}
