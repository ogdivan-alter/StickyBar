using StickyDock.Core.Mvvm;

namespace StickyDock.Tests.Mvvm;

/// <summary>
/// Change notification of <see cref="ViewModelBase"/> through <see cref="ObservableObject"/>.
/// </summary>
public sealed class ObservableObjectTests
{
    [Fact]
    public void SetProperty_WhenTheValueChanges_RaisesPropertyChangedWithThePropertyName()
    {
        var viewModel = new SampleViewModel();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raisedProperties.Add(arguments.PropertyName);

        viewModel.Name = "updated";

        Assert.Equal(new[] { nameof(SampleViewModel.Name) }, raisedProperties);
    }

    [Fact]
    public void SetProperty_WhenTheValueIsUnchanged_DoesNotRaisePropertyChanged()
    {
        var viewModel = new SampleViewModel();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raisedProperties.Add(arguments.PropertyName);

        viewModel.Name = "initial";

        Assert.Empty(raisedProperties);
    }

    [Fact]
    public void SetProperty_ReportsWhetherTheValueChanged()
    {
        var viewModel = new SampleViewModel();

        Assert.True(viewModel.TryRename("changed"));
        Assert.False(viewModel.TryRename("changed"));
    }

    [Fact]
    public void OnPropertyChanged_WhenCalledExplicitly_RaisesTheGivenProperty()
    {
        var viewModel = new SampleViewModel();
        var raisedProperties = new List<string?>();
        viewModel.PropertyChanged += (_, arguments) => raisedProperties.Add(arguments.PropertyName);

        viewModel.NotifyCustom();

        Assert.Equal(new[] { "Custom" }, raisedProperties);
    }

    private sealed class SampleViewModel : ViewModelBase
    {
        private string _name = "initial";

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public bool TryRename(string name) => SetProperty(ref _name, name, nameof(Name));

        public void NotifyCustom() => OnPropertyChanged("Custom");
    }
}
