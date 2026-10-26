namespace StickyDock.Core.Mvvm;

/// <summary>
/// Base class for view models. It carries the observable-change contract so that views stay free of state logic
/// (see §5.2-01 of the specification and AGENTS.md).
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
}
