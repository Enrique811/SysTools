using SysTools.Presentation.Commands;
using SysTools.Presentation.ViewModels;

namespace SysTools.Presentation.Tests.Mvvm;

public sealed class MvvmInfrastructureTests
{
    [Fact]
    public void Relay_command_honors_availability_executes_and_notifies()
    {
        var executed = false;
        var available = false;
        var notifications = 0;
        var command = new RelayCommand(() => executed = true, () => available);
        command.CanExecuteChanged += (_, _) => notifications++;
        Assert.False(command.CanExecute(null));
        available = true;
        command.NotifyCanExecuteChanged();
        command.Execute(null);
        Assert.True(executed);
        Assert.Equal(1, notifications);
    }

    [Fact]
    public void Base_view_model_notifies_only_when_value_changes()
    {
        var vm = new ProbeViewModel();
        var changes = new List<string?>();
        vm.PropertyChanged += (_, e) => changes.Add(e.PropertyName);
        vm.Value = "nuevo";
        vm.Value = "nuevo";
        Assert.Equal(new[] { nameof(vm.Value) }, changes);
    }

    private sealed class ProbeViewModel : ViewModelBase
    {
        private string _value = string.Empty;
        public string Value { get => _value; set => SetProperty(ref _value, value); }
    }
}
