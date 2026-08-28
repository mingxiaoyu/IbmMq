using System;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.Data;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;

namespace MqManager.Desktop.ViewModels;

public partial class ServerSettingsViewModel : ViewModelBase
{
    private readonly ConfigRepository      _configRepo;
    private readonly PreferencesRepository? _prefs;
    private Action? _closeAction;

    [ObservableProperty] private ServerConfig? _selectedServer;
    [ObservableProperty] private ServerConfig? _editingConfig;
    [ObservableProperty] private bool          _isEditing;

    // ── Translated labels ────────────────────────────────────────────────────
    [ObservableProperty] private string _title             = "Server Settings";
    [ObservableProperty] private string _serversLabel      = "Servers";
    [ObservableProperty] private string _newText           = "New";
    [ObservableProperty] private string _editText          = "Edit";
    [ObservableProperty] private string _deleteText        = "Delete";
    [ObservableProperty] private string _saveText          = "Save";
    [ObservableProperty] private string _cancelText        = "Cancel";
    [ObservableProperty] private string _closeText         = "Close";
    [ObservableProperty] private string _selectServerText  = "Select a server or click New";
    [ObservableProperty] private string _serverConfigText  = "Server Configuration";
    [ObservableProperty] private string _nameLabel         = "Name";
    [ObservableProperty] private string _hostLabel         = "Host";
    [ObservableProperty] private string _portLabel         = "Port";
    [ObservableProperty] private string _channelLabel      = "Channel";
    [ObservableProperty] private string _queueManagerLabel = "Queue Manager";
    [ObservableProperty] private string _usernameLabel     = "Username";
    [ObservableProperty] private string _passwordLabel     = "Password";
    [ObservableProperty] private string _serverNameWatermark = "Server name";
    [ObservableProperty] private string _hostnameWatermark   = "hostname or IP";
    [ObservableProperty] private string _portWatermark       = "1414";
    [ObservableProperty] private string _channelWatermark    = "Channel name";
    [ObservableProperty] private string _qmNameWatermark     = "QM name";
    [ObservableProperty] private string _optionalWatermark   = "Optional";

    // ── General / Close-behavior ─────────────────────────────────────────────
    [ObservableProperty] private string _generalSectionText   = "General";
    [ObservableProperty] private string _closeBehaviorLabel   = "Close Button Behavior";

    /// <summary>Localised option labels shown in the ComboBox.</summary>
    public ObservableCollection<string> CloseBehaviorOptions { get; } = [];

    /// <summary>0 = Ask, 1 = Minimize to tray, 2 = Exit.</summary>
    [ObservableProperty]
    private int _closeBehaviorIndex;

    partial void OnCloseBehaviorIndexChanged(int value)
    {
        var behavior = value switch { 1 => "minimize", 2 => "exit", _ => "ask" };
        _prefs?.Set("close_behavior", behavior);
    }

    // ────────────────────────────────────────────────────────────────────────

    public ObservableCollection<ServerConfig> Servers { get; } = [];
    public event Action? ServerSaved;

    public ServerSettingsViewModel(ConfigRepository configRepo,
                                   PreferencesRepository? prefs = null)
    {
        _configRepo = configRepo;
        _prefs      = prefs;

        // Initialise close-behavior index from persisted preference
        _closeBehaviorIndex = _prefs?.Get("close_behavior") switch
        {
            "minimize" => 1,
            "exit"     => 2,
            _          => 0   // null or "ask"
        };

        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        LoadServers();
        UpdateStrings();
    }

    public void SetCloseAction(Action close) => _closeAction = close;

    public void LoadServers()
    {
        Servers.Clear();
        foreach (var config in _configRepo.GetAll())
            Servers.Add(config);

        SelectedServer = Servers.Count > 0 ? Servers[0] : null;
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        Title             = Strings.Get("Settings_Title");
        ServersLabel      = Strings.Get("Settings_Servers");
        NewText           = Strings.Get("Settings_New");
        EditText          = Strings.Get("Settings_Edit");
        DeleteText        = Strings.Get("Settings_Delete");
        SaveText          = Strings.Get("Settings_Save");
        CancelText        = Strings.Get("Settings_Cancel");
        CloseText         = Strings.Get("Settings_Close");
        SelectServerText  = Strings.Get("Settings_SelectServer");
        ServerConfigText  = Strings.Get("Settings_ServerConfig");
        NameLabel         = Strings.Get("Settings_Name");
        HostLabel         = Strings.Get("Settings_Host");
        PortLabel         = Strings.Get("Settings_Port");
        ChannelLabel      = Strings.Get("Settings_Channel");
        QueueManagerLabel = Strings.Get("Settings_QueueManager");
        UsernameLabel     = Strings.Get("Settings_Username");
        PasswordLabel     = Strings.Get("Settings_Password");
        ServerNameWatermark = Strings.Get("Settings_WM_ServerName");
        HostnameWatermark   = Strings.Get("Settings_WM_Hostname");
        PortWatermark       = Strings.Get("Settings_WM_Port");
        ChannelWatermark    = Strings.Get("Settings_WM_Channel");
        QmNameWatermark     = Strings.Get("Settings_WM_QmName");
        OptionalWatermark   = Strings.Get("Settings_WM_Optional");

        GeneralSectionText = Strings.Get("Settings_GeneralSection");
        CloseBehaviorLabel = Strings.Get("Settings_CloseBehaviorLabel");

        // Rebuild options collection; SelectedIndex is preserved by Avalonia
        var saved = CloseBehaviorIndex;
        CloseBehaviorOptions.Clear();
        CloseBehaviorOptions.Add(Strings.Get("Settings_Close_Ask"));
        CloseBehaviorOptions.Add(Strings.Get("Settings_Close_Minimize"));
        CloseBehaviorOptions.Add(Strings.Get("Settings_Close_Exit"));
        CloseBehaviorIndex = saved;
    }

    [RelayCommand]
    private void NewServer()
    {
        EditingConfig = new ServerConfig();
        IsEditing     = true;
    }

    [RelayCommand]
    private void EditServer()
    {
        if (SelectedServer is null) return;
        EditingConfig = new ServerConfig
        {
            Id           = SelectedServer.Id,
            Name         = SelectedServer.Name,
            Host         = SelectedServer.Host,
            Port         = SelectedServer.Port,
            Channel      = SelectedServer.Channel,
            QueueManager = SelectedServer.QueueManager,
            Username     = SelectedServer.Username,
            Password     = SelectedServer.Password
        };
        IsEditing = true;
    }

    [RelayCommand]
    private void SaveServer()
    {
        if (EditingConfig is null) return;
        _configRepo.Save(EditingConfig);
        IsEditing     = false;
        EditingConfig = null;
        LoadServers();
        ServerSaved?.Invoke();
    }

    [RelayCommand]
    private void CancelEdit()
    {
        IsEditing     = false;
        EditingConfig = null;
    }

    [RelayCommand]
    private void DeleteServer()
    {
        if (SelectedServer is null) return;
        _configRepo.Delete(SelectedServer.Id);
        LoadServers();
        ServerSaved?.Invoke();
    }

    [RelayCommand]
    private void Close() => _closeAction?.Invoke();
}

