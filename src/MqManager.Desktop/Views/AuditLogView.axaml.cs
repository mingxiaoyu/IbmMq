using Avalonia.Markup.Xaml;

namespace MqManager.Desktop.Views;

public partial class AuditLogView : Avalonia.Controls.UserControl
{
    public AuditLogView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
