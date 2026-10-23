using Avalonia.Controls;
using Avalonia.Input;
using MqManager.Desktop.ViewModels;

namespace MqManager.Desktop.Views;

public partial class ServerSettingsView : Window
{
    public ServerSettingsView()
    {
        InitializeComponent();
    }

    private void OnServerDoubleTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is ServerSettingsViewModel vm && vm.SelectedServer is not null)
            vm.EditServerCommand.Execute(null);
    }
}
