using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.Data;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private enum StatusType { None, PleaseSelect, Connecting, ConnectedTo }

    private readonly ConfigRepository _configRepo;
    private readonly IMqService _mqService;
    private StatusType _currentStatusType = StatusType.None;
    private string _connectedServerName = string.Empty;

    public ConfigRepository ConfigRepo => _configRepo;

    [ObservableProperty]
    private ServerConfig? _selectedServer;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isConnecting;

    [ObservableProperty]
    private bool _showSettings;

    [ObservableProperty]
    private string _currentLanguage = "EN";

    // Translated strings
    [ObservableProperty]
    private string _title = "MQ Connection Manager";

    [ObservableProperty]
    private string _serverLabel = "Server";

    [ObservableProperty]
    private string _connectText = "Connect";

    [ObservableProperty]
    private string _settingsText = "Server Settings";

    [ObservableProperty]
    private string _pleaseSelectServer = "Please select a server";

    [ObservableProperty]
    private string _connecting = "Connecting...";

    public ObservableCollection<ServerConfig> Servers { get; } = [];

    public event Action? Connected;

    public LoginViewModel(ConfigRepository configRepo, IMqService mqService)
    {
        _configRepo = configRepo;
        _mqService = mqService;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        LoadServers();
        UpdateStrings();
    }

    public void LoadServers()
    {
        Servers.Clear();
        foreach (var config in _configRepo.GetAll())
            Servers.Add(config);

        SelectedServer = Servers.Count > 0 ? Servers[0] : null;
    }

    private void OnCultureChanged()
    {
        UpdateStrings();
        CurrentLanguage = CultureHelper.Instance.CurrentCulture.TwoLetterISOLanguageName == "zh" ? "中文" : "EN";
    }

    private void UpdateStrings()
    {
        Title = Strings.Get("Login_Title");
        ServerLabel = Strings.Get("Login_ServerLabel");
        ConnectText = Strings.Get("Login_Connect");
        SettingsText = Strings.Get("Settings_Title");
        PleaseSelectServer = Strings.Get("Login_PleaseSelect");
        Connecting = Strings.Get("Login_Connecting");

        StatusMessage = _currentStatusType switch
        {
            StatusType.PleaseSelect => PleaseSelectServer,
            StatusType.Connecting => Connecting,
            StatusType.ConnectedTo => Strings.Format("Login_ConnectedTo", _connectedServerName),
            _ => StatusMessage
        };
    }

    [RelayCommand]
    private async Task ConnectAsync()
    {
        if (SelectedServer is null)
        {
            _currentStatusType = StatusType.PleaseSelect;
            StatusMessage = PleaseSelectServer;
            return;
        }

        IsConnecting = true;
        _currentStatusType = StatusType.Connecting;
        StatusMessage = Connecting;

        try
        {
            await Task.Run(() => _mqService.Connect(SelectedServer));
            _currentStatusType = StatusType.ConnectedTo;
            _connectedServerName = SelectedServer.Name;
            StatusMessage = Strings.Format("Login_ConnectedTo", SelectedServer.Name);
            Connected?.Invoke();
        }
        catch (Exception ex)
        {
            _currentStatusType = StatusType.None;
            StatusMessage = ex.Message;
        }
        finally
        {
            IsConnecting = false;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        ShowSettings = true;
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        CultureHelper.Instance.ToggleLanguage();
    }
}
