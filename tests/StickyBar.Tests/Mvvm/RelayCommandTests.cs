using StickyBar.Core.Mvvm;

namespace StickyBar.Tests.Mvvm;

/// <summary>
/// Execution and availability contract of <see cref="RelayCommand"/>.
/// </summary>
public sealed class RelayCommandTests
{
    [Fact]
    public void Execute_InvokesTheCallback()
    {
        var executionCount = 0;
        var command = new RelayCommand(() => executionCount++);

        command.Execute(parameter: null);

        Assert.Equal(1, executionCount);
    }

    [Fact]
    public void CanExecute_WhenNoPredicateIsProvided_IsTrue()
    {
        var command = new RelayCommand(() => { });

        Assert.True(command.CanExecute(parameter: null));
    }

    [Fact]
    public void CanExecute_FollowsThePredicate()
    {
        var isEnabled = false;
        var command = new RelayCommand(() => { }, () => isEnabled);

        Assert.False(command.CanExecute(parameter: null));

        isEnabled = true;

        Assert.True(command.CanExecute(parameter: null));
    }

    [Fact]
    public void RaiseCanExecuteChanged_NotifiesTheSubscribers()
    {
        var command = new RelayCommand(() => { });
        var notificationCount = 0;
        command.CanExecuteChanged += (_, _) => notificationCount++;

        command.RaiseCanExecuteChanged();

        Assert.Equal(1, notificationCount);
    }

    [Fact]
    public void Constructor_WhenTheCallbackIsMissing_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
    }
}
