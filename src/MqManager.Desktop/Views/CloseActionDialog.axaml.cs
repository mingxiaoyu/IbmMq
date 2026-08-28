using Avalonia.Controls;
using Avalonia.Interactivity;

namespace MqManager.Desktop.Views;

public partial class CloseActionDialog : Window
{
    /// <summary>"minimize" to hide to tray, "exit" to quit, null if cancelled.</summary>
    public string? SelectedAction { get; private set; }

    public bool ShouldRemember => RememberOption.IsChecked == true;

    public CloseActionDialog() => InitializeComponent();

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        SelectedAction = MinimizeOption.IsChecked == true ? "minimize" : "exit";
        Close();
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        SelectedAction = null;
        Close();
    }
}
