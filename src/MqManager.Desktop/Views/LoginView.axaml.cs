using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MqManager.Desktop.ViewModels;

namespace MqManager.Desktop.Views;

public partial class LoginView : UserControl
{
    public LoginView()
    {
        InitializeComponent();
    }

    private async void OnOpenSettings(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var vm = DataContext as LoginViewModel;
        if (vm == null) return;

        var window     = new ServerSettingsView();
        var prefs      = new MqManager.Desktop.Data.PreferencesRepository(
                             new MqManager.Desktop.Data.AppDbContext());
        var settingsVm = new ServerSettingsViewModel(vm.ConfigRepo, prefs);
        settingsVm.ServerSaved += () =>
        {
            vm.LoadServers();
            window.Close();
        };
        settingsVm.SetCloseAction(() => window.Close());
        window.DataContext = settingsVm;

        var parentWindow = TopLevel.GetTopLevel(this) as Window;
        if (parentWindow != null)
            await window.ShowDialog(parentWindow);
    }
}
