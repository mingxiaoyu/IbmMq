using CommunityToolkit.Mvvm.ComponentModel;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;

namespace MqManager.Desktop.ViewModels;

public partial class ConnectionDetailViewModel : ViewModelBase
{
    [ObservableProperty]
    private string _channelName = string.Empty;

    [ObservableProperty]
    private string _connectionName = string.Empty;

    [ObservableProperty]
    private string _appName = string.Empty;

    [ObservableProperty]
    private string _userId = string.Empty;

    [ObservableProperty]
    private string _status = string.Empty;

    // Translated strings
    [ObservableProperty]
    private string _channelLabel = "Channel Name:";

    [ObservableProperty]
    private string _connectionLabel = "Connection:";

    [ObservableProperty]
    private string _appNameLabel = "App Name:";

    [ObservableProperty]
    private string _userIdLabel = "User ID:";

    [ObservableProperty]
    private string _statusLabel = "Status:";

    [ObservableProperty]
    private string _detailsTitle = "Connection Details";

    public ConnectionDetailViewModel()
    {
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        ChannelLabel = Strings.Get("ConnectionDetail_Channel");
        ConnectionLabel = Strings.Get("ConnectionDetail_Connection");
        AppNameLabel = Strings.Get("ConnectionDetail_AppName");
        UserIdLabel = Strings.Get("ConnectionDetail_UserId");
        StatusLabel = Strings.Get("ConnectionDetail_Status");
        DetailsTitle = Strings.Get("ConnectionDetail_Title");
    }

    public void SetConnection(ConnectionInfo connection)
    {
        ChannelName = connection.ChannelName;
        ConnectionName = connection.ConnectionName;
        AppName = connection.AppName;
        UserId = connection.UserId;
        Status = connection.Status;
    }
}
