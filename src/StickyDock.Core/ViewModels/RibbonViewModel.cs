using System.Windows.Input;
using StickyDock.Core.Mvvm;
using StickyDock.Core.Ribbons;

namespace StickyDock.Core.ViewModels;

/// <summary>
/// View model for a single flat <see cref="Ribbon"/> on the dock bar (E4).
/// </summary>
public sealed class RibbonViewModel : ViewModelBase
{
    private readonly Action<string> _toggle;
    private readonly Func<string, string, bool> _rename;
    private readonly Func<string, bool> _delete;
    private readonly Func<string, bool> _moveEarlier;
    private readonly Func<string, bool> _moveLater;

    private string _name;
    private int _order;
    private IReadOnlyList<RibbonItem> _items;
    private bool _isOpen;
    private bool _isRenaming;
    private string _draftName;

    /// <summary>
    /// Initializes a new instance of the <see cref="RibbonViewModel"/> class.
    /// </summary>
    public RibbonViewModel(
        Ribbon model,
        Action<string> toggle,
        Func<string, string, bool> rename,
        Func<string, bool> delete,
        Func<string, bool> moveEarlier,
        Func<string, bool> moveLater)
    {
        ArgumentNullException.ThrowIfNull(model);

        Id = model.Id;
        _name = model.Name;
        _draftName = model.Name;
        _order = model.Order;
        _items = model.Items;

        _toggle = toggle ?? throw new ArgumentNullException(nameof(toggle));
        _rename = rename ?? throw new ArgumentNullException(nameof(rename));
        _delete = delete ?? throw new ArgumentNullException(nameof(delete));
        _moveEarlier = moveEarlier ?? throw new ArgumentNullException(nameof(moveEarlier));
        _moveLater = moveLater ?? throw new ArgumentNullException(nameof(moveLater));

        ToggleCommand = new RelayCommand(() => _toggle(Id));
        BeginRenameCommand = new RelayCommand(BeginRename);
        CommitRenameCommand = new RelayCommand(CommitRename);
        CancelRenameCommand = new RelayCommand(CancelRename);
        DeleteCommand = new RelayCommand(() => _delete(Id));
        MoveEarlierCommand = new RelayCommand(() => _moveEarlier(Id));
        MoveLaterCommand = new RelayCommand(() => _moveLater(Id));
    }

    /// <summary>Gets the unique identifier of the ribbon.</summary>
    public string Id { get; }

    /// <summary>Gets the display name of the ribbon.</summary>
    public string Name
    {
        get => _name;
        internal set => SetProperty(ref _name, value);
    }

    /// <summary>Gets the zero-based position of the ribbon along the dock bar.</summary>
    public int Order
    {
        get => _order;
        internal set => SetProperty(ref _order, value);
    }

    /// <summary>Gets the flat shortcut items contained in this ribbon.</summary>
    public IReadOnlyList<RibbonItem> Items
    {
        get => _items;
        internal set
        {
            if (SetProperty(ref _items, value))
            {
                OnPropertyChanged(nameof(ItemCount));
            }
        }
    }

    /// <summary>Gets the number of shortcut items in this ribbon.</summary>
    public int ItemCount => _items.Count;

    /// <summary>Gets a value indicating whether this ribbon is currently open (transient, Q-10).</summary>
    public bool IsOpen
    {
        get => _isOpen;
        internal set => SetProperty(ref _isOpen, value);
    }

    /// <summary>Gets a value indicating whether the inline rename editor is active.</summary>
    public bool IsRenaming
    {
        get => _isRenaming;
        private set => SetProperty(ref _isRenaming, value);
    }

    /// <summary>Gets or sets the draft name edited in the UI before committing.</summary>
    public string DraftName
    {
        get => _draftName;
        set => SetProperty(ref _draftName, value);
    }

    /// <summary>Gets the command that toggles this ribbon open or closed (E4-CA2).</summary>
    public ICommand ToggleCommand { get; }

    /// <summary>Gets the command that enters inline rename mode.</summary>
    public ICommand BeginRenameCommand { get; }

    /// <summary>Gets the command that commits the edited <see cref="DraftName"/>.</summary>
    public ICommand CommitRenameCommand { get; }

    /// <summary>Gets the command that cancels inline rename mode.</summary>
    public ICommand CancelRenameCommand { get; }

    /// <summary>Gets the command that requests deletion of this ribbon (E4-CA5, Q-07).</summary>
    public ICommand DeleteCommand { get; }

    /// <summary>Gets the command that moves this ribbon one slot earlier.</summary>
    public ICommand MoveEarlierCommand { get; }

    /// <summary>Gets the command that moves this ribbon one slot later.</summary>
    public ICommand MoveLaterCommand { get; }

    internal Ribbon ToModel() => new()
    {
        Id = Id,
        Name = Name,
        Order = Order,
        Items = Items,
    };

    private void BeginRename()
    {
        DraftName = Name;
        IsRenaming = true;
    }

    private void CommitRename()
    {
        _rename(Id, DraftName);
        IsRenaming = false;
    }

    private void CancelRename()
    {
        DraftName = Name;
        IsRenaming = false;
    }
}
