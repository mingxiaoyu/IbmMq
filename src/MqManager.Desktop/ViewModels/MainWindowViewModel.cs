using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.Data;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;
using MqManager.Desktop.i18n;
using MqManager.Desktop.Views;
using MqManager.Core.Models;

namespace MqManager.Desktop.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _currentLanguage = "EN";

    [ObservableProperty]
    private string _windowTitle = "IBM MQ Desktop Client";

    [ObservableProperty]
    private string _appTitle = "IBM MQ Desktop Client";

    [ObservableProperty]
    private string _serverName = string.Empty;

    public string DisplayTitle => string.IsNullOrEmpty(ServerName) ? AppTitle : ServerName;

    [ObservableProperty]
    private Control? _loginView;

    [ObservableProperty]
    private Control? _sidebarContent;

    [ObservableProperty]
    private Control? _contentPanel;

    [ObservableProperty]
    private int _activeTabIndex;

    [ObservableProperty]
    private string _queuesTab = "Queues";

    [ObservableProperty]
    private string _channelsTab = "Channels";

    [ObservableProperty]
    private string _connectionsTab = "Connections";

    [ObservableProperty]
    private string _auditLogTab = "Log";

    [ObservableProperty]
    private string _disconnectText = "Disconnect";

    [ObservableProperty]
    private string _disconnectTooltip = "Disconnect";

    [ObservableProperty]
    private string _themeIcon = "☀";

    [ObservableProperty]
    private string _themeTooltip = "Switch to Dark Mode";

    private bool _isDarkMode;

    public LoginViewModel LoginVm { get; }
    public ServerSettingsViewModel SettingsVm { get; }
    public QueueListViewModel QueueListVm { get; }
    public QueueActionsViewModel QueueActionsVm { get; }
    public MessagePanelViewModel MessagePanelVm { get; }
    public ChannelListViewModel ChannelListVm { get; }
    public ChannelStatusViewModel ChannelStatusVm { get; }
    public ConnectionListViewModel ConnectionListVm { get; }
    public ConnectionDetailViewModel ConnectionDetailVm { get; }
    public AuditLogViewModel AuditLogVm { get; }

    private readonly PreferencesRepository _prefs;

    // Per-tab content state — each tab remembers its last ContentPanel
    private readonly Control?[] _tabContent = new Control?[4];
    // Lazy load guard — each tab's data is loaded only on first visit
    private readonly bool[] _tabInitialized = new bool[4];
    private int _previousTabIndex = -1;

    // Cached view instances (pre-created to avoid re-render flash on tab switch)
    private QueueListView? _queueListView;
    private ChannelListView? _channelListView;
    private ConnectionListView? _connectionListView;
    private AuditLogView? _auditLogView;
    private MessagePanelView? _messagePanelView;
    private ChannelStatusView? _channelStatusView;
    private ConnectionDetailView? _connectionDetailView;

    public MainWindowViewModel()
    {
        var appDbContext = new AppDbContext();
        var configRepo = new ConfigRepository(appDbContext);
        var cacheRepo = new CacheRepository(appDbContext);
        var mqService = new MqService();
        _prefs = new PreferencesRepository(appDbContext);

        LoginVm    = new LoginViewModel(configRepo, mqService);
        SettingsVm = new ServerSettingsViewModel(configRepo, _prefs);
        QueueListVm = new QueueListViewModel(mqService, cacheRepo);
        QueueActionsVm = new QueueActionsViewModel(mqService);
        MessagePanelVm = new MessagePanelViewModel(mqService);
        ChannelListVm = new ChannelListViewModel(mqService);
        ChannelStatusVm = new ChannelStatusViewModel(mqService);
        ConnectionListVm = new ConnectionListViewModel(mqService);
        ConnectionDetailVm = new ConnectionDetailViewModel();
        AuditLogVm = new AuditLogViewModel();

        LoginVm.Connected += OnConnected;
        SettingsVm.ServerSaved += OnServerSaved;

        QueueListVm.QueueSelected += QueueSelected;
        ChannelListVm.ChannelSelected += ChannelSelected;
        ConnectionListVm.ConnectionSelected += ConnectionSelected;

        QueueActionsVm.SetRefreshAction(() => QueueListVm.RefreshCommand.Execute(null));

        // Log connect/disconnect to audit log
        LoginVm.Connected += () => AuditLogService.Instance.Log("System", "Connect",
            LoginVm.SelectedServer?.Name ?? "unknown", true);

        CultureHelper.Instance.CultureChanged += (_, _) => UpdateStrings();
        UpdateStrings();

        // Restore saved theme
        var savedTheme = _prefs.Get("theme");
        if (savedTheme == "dark")
            ApplyTheme(dark: true, save: false);

        var loginView = new LoginView { DataContext = LoginVm };
        LoginView = loginView;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        ApplyTheme(!_isDarkMode, save: true);
    }

    private void ApplyTheme(bool dark, bool save)
    {
        _isDarkMode = dark;
        if (Application.Current is not null)
            Application.Current.RequestedThemeVariant = dark ? ThemeVariant.Dark : ThemeVariant.Light;
        ThemeIcon = dark ? "☾" : "☀";
        ThemeTooltip = dark ? "Switch to Light Mode" : "Switch to Dark Mode";
        if (save)
            _prefs.Set("theme", dark ? "dark" : "light");
    }

    [RelayCommand]
    private void ToggleLanguage()
    {
        CultureHelper.Instance.ToggleLanguage();
    }

    [RelayCommand]
    private void Disconnect()
    {
        var serverName = ServerName;
        IsConnected = false;
        ServerName = string.Empty;
        OnPropertyChanged(nameof(DisplayTitle));

        // Reset navigation state
        Array.Clear(_tabContent, 0, _tabContent.Length);
        Array.Clear(_tabInitialized, 0, _tabInitialized.Length);
        _previousTabIndex = -1;
        ActiveTabIndex = 0;
        SidebarContent = null;
        ContentPanel = null;

        LoginView = new LoginView { DataContext = LoginVm };
        LoginVm.LoadServers();
        AuditLogService.Instance.Log("System", "Disconnect", serverName, true);
    }

    private void UpdateStrings()
    {
        var isZh = CultureHelper.Instance.CurrentCulture.TwoLetterISOLanguageName == "zh";
        CurrentLanguage = isZh ? "中文" : "EN";
        WindowTitle = Strings.Get("Window_Title");
        AppTitle = Strings.Get("App_Title");
        QueuesTab = Strings.Get("Tab_Queues");
        ChannelsTab = Strings.Get("Tab_Channels");
        ConnectionsTab = Strings.Get("Tab_Connections");
        AuditLogTab = Strings.Get("Tab_AuditLog");
        DisconnectText = Strings.Get("Window_Disconnect");
        DisconnectTooltip = Strings.Get("Window_Disconnect");
    }

    partial void OnActiveTabIndexChanged(int value)
    {
        if (!IsConnected) return;

        // Save current content to previous tab's slot
        if (_previousTabIndex is >= 0 and < 4)
            _tabContent[_previousTabIndex] = ContentPanel;
        _previousTabIndex = value;

        if (value == 3)
        {
            SidebarContent = null;
            _auditLogView ??= new AuditLogView { DataContext = AuditLogVm };
            ContentPanel = _auditLogView;
            return;
        }

        // Restore sidebar (cached view)
        LoadSidebarCommand.Execute(null);

        // Restore last content for this tab (null = no item selected yet)
        ContentPanel = _tabContent[value];

        // Load data only on first visit to avoid redundant CLI calls
        if (_tabInitialized[value]) return;
        _tabInitialized[value] = true;

        switch (value)
        {
            case 0: QueueListVm.LoadQueuesCommand.Execute(null); break;
            case 1: ChannelListVm.LoadChannelsCommand.Execute(null); break;
            case 2: ConnectionListVm.LoadConnectionsCommand.Execute(null); break;
        }
    }

    [RelayCommand]
    private void LoadSidebar()
    {
        if (ActiveTabIndex == 3) return;

        _queueListView ??= new QueueListView { DataContext = QueueListVm };
        _channelListView ??= new ChannelListView { DataContext = ChannelListVm };
        _connectionListView ??= new ConnectionListView { DataContext = ConnectionListVm };

        SidebarContent = ActiveTabIndex switch
        {
            0 => _queueListView,
            1 => _channelListView,
            2 => _connectionListView,
            _ => _queueListView
        };
    }

    private void OnConnected()
    {
        IsConnected = true;
        LoginView = null;
        ServerName = LoginVm.SelectedServer?.Name ?? AppTitle;
        OnPropertyChanged(nameof(DisplayTitle));

        // Reset per-tab state for new connection
        Array.Clear(_tabContent, 0, _tabContent.Length);
        Array.Clear(_tabInitialized, 0, _tabInitialized.Length);
        _previousTabIndex = -1;

        // Init Tab 0 (Queues) as starting point
        ActiveTabIndex = 0;
        _tabInitialized[0] = true;

        _queueListView ??= new QueueListView { DataContext = QueueListVm };
        SidebarContent = _queueListView;

        _messagePanelView ??= new MessagePanelView { DataContext = MessagePanelVm };
        ContentPanel = _messagePanelView;
        _tabContent[0] = _messagePanelView;

        if (LoginVm.SelectedServer is { } server)
            QueueListVm.SetServer(server);
        QueueListVm.LoadQueuesCommand.Execute(null);
    }

    private void OnServerSaved()
    {
        LoginVm.LoadServers();
        LoginVm.ShowSettings = false;
    }

    private void QueueSelected(QueueInfo queue)
    {
        MessagePanelVm.SetQueue(queue);
        _messagePanelView ??= new MessagePanelView { DataContext = MessagePanelVm };
        ContentPanel = _messagePanelView;
        _tabContent[0] = _messagePanelView;
    }

    private void ChannelSelected(ChannelInfo channel)
    {
        ChannelStatusVm.SetChannel(channel.Name);
        _channelStatusView ??= new ChannelStatusView { DataContext = ChannelStatusVm };
        ContentPanel = _channelStatusView;
        _tabContent[1] = _channelStatusView;
    }

    private void ConnectionSelected(ConnectionInfo connection)
    {
        ConnectionDetailVm.SetConnection(connection);
        _connectionDetailView ??= new ConnectionDetailView { DataContext = ConnectionDetailVm };
        ContentPanel = _connectionDetailView;
        _tabContent[2] = _connectionDetailView;
    }
}
