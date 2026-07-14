using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class ChannelStatusViewModel : ViewModelBase
{
    private readonly IMqService _mqService;

    [ObservableProperty]
    private string _channelName = string.Empty;

    [ObservableProperty]
    private string _resultText = string.Empty;

    [ObservableProperty]
    private bool _isForceStop;

    [ObservableProperty]
    private bool _isForceReset;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private int _activeTabIndex;

    // Status fields
    [ObservableProperty]
    private string _statusName = string.Empty;

    [ObservableProperty]
    private string _statusType = string.Empty;

    [ObservableProperty]
    private string _statusStatus = string.Empty;

    [ObservableProperty]
    private string _statusConnectionName = string.Empty;

    [ObservableProperty]
    private long _statusBytesSent;

    [ObservableProperty]
    private long _statusBytesReceived;

    [ObservableProperty]
    private int _statusMessages;

    [ObservableProperty]
    private int _statusBuffersSent;

    [ObservableProperty]
    private int _statusBuffersReceived;

    // Tab labels
    [ObservableProperty]
    private string _statusTabText = "Status";

    [ObservableProperty]
    private string _connInfoTabText = "Connection Info";

    // Translated strings
    [ObservableProperty]
    private string _refreshText = "Refresh";

    [ObservableProperty]
    private string _startText = "Start";

    [ObservableProperty]
    private string _stopText = "Stop";

    [ObservableProperty]
    private string _resetText = "Reset";

    [ObservableProperty]
    private string _forceStopText = "Force Stop";

    [ObservableProperty]
    private string _forceResetText = "Force Reset";

    [ObservableProperty]
    private string _nameLabel = "Name";

    [ObservableProperty]
    private string _typeLabel = "Type";

    [ObservableProperty]
    private string _statusLabel = "Status";

    [ObservableProperty]
    private string _connectionNameLabel = "Connection Name";

    [ObservableProperty]
    private string _bytesSentLabel = "Bytes Sent";

    [ObservableProperty]
    private string _bytesReceivedLabel = "Bytes Received";

    [ObservableProperty]
    private string _messagesLabel = "Messages";

    [ObservableProperty]
    private string _buffersSentLabel = "Buffers Sent";

    [ObservableProperty]
    private string _buffersReceivedLabel = "Buffers Received";

    [ObservableProperty]
    private string _loadingText = "Loading...";

    // ConnInfo labels
    [ObservableProperty]
    private string _connQueueManagerLabel = "Queue Manager";

    [ObservableProperty]
    private string _connTransmissionQueueLabel = "Transmission Queue";

    [ObservableProperty]
    private string _connSubstateLabel = "Substate";

    [ObservableProperty]
    private string _connIndoubtStatusLabel = "Indoubt Status";

    [ObservableProperty]
    private string _connMessageCompressionLabel = "Message Compression";

    [ObservableProperty]
    private string _connHeaderCompressionLabel = "Header Compression";

    [ObservableProperty]
    private string _connMcaUserIdLabel = "MCA User ID";

    public ObservableCollection<ChannelConnectionInfo> ConnInfos { get; } = [];

    public ChannelStatusViewModel(IMqService mqService)
    {
        _mqService = mqService;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        RefreshText = Strings.Get("Common_Refresh");
        StartText = Strings.Get("Channel_Start");
        StopText = Strings.Get("Channel_Stop");
        ResetText = Strings.Get("Channel_Reset");
        ForceStopText = Strings.Get("Channel_ForceStop");
        ForceResetText = Strings.Get("Channel_ForceReset");
        NameLabel = Strings.Get("Channel_NameHeader");
        TypeLabel = Strings.Get("Channel_TypeHeader");
        StatusLabel = Strings.Get("Channel_StatusHeader");
        ConnectionNameLabel = Strings.Get("Channel_ConnectionHeader");
        BytesSentLabel = Strings.Get("Channel_BytesSent");
        BytesReceivedLabel = Strings.Get("Channel_BytesReceived");
        MessagesLabel = Strings.Get("Channel_Messages");
        BuffersSentLabel = Strings.Get("Channel_BuffersSent");
        BuffersReceivedLabel = Strings.Get("Channel_BuffersReceived");
        StatusTabText = Strings.Get("Channel_Status");
        ConnInfoTabText = Strings.Get("Channel_ConnInfo");
        ConnQueueManagerLabel = Strings.Get("Channel_ConnQueueManager");
        ConnTransmissionQueueLabel = Strings.Get("Channel_ConnTransmissionQueue");
        ConnSubstateLabel = Strings.Get("Channel_ConnSubstate");
        ConnIndoubtStatusLabel = Strings.Get("Channel_ConnIndoubtStatus");
        ConnMessageCompressionLabel = Strings.Get("Channel_ConnMessageCompression");
        ConnHeaderCompressionLabel = Strings.Get("Channel_ConnHeaderCompression");
        ConnMcaUserIdLabel = Strings.Get("Channel_McaUserHeader");
        LoadingText = Strings.Get("Common_Loading");
    }

    public void SetChannel(string channelName)
    {
        ChannelName = channelName;
        ClearStatus();
        ConnInfos.Clear();
        ActiveTabIndex = 0;
        RefreshCommand.Execute(null);
    }

    partial void OnActiveTabIndexChanged(int value)
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;
        if (value == 1 && ConnInfos.Count == 0)
            LoadConnInfoCommand.Execute(null);
    }

    private void ClearStatus()
    {
        StatusName = string.Empty;
        StatusType = string.Empty;
        StatusStatus = string.Empty;
        StatusConnectionName = string.Empty;
        StatusBytesSent = 0;
        StatusBytesReceived = 0;
        StatusMessages = 0;
        StatusBuffersSent = 0;
        StatusBuffersReceived = 0;
    }

    private void PopulateStatus(ChannelStatus status)
    {
        StatusName = status.Name;
        StatusType = status.Type;
        StatusStatus = status.Status;
        StatusConnectionName = status.ConnectionName;
        StatusBytesSent = status.BytesSent;
        StatusBytesReceived = status.BytesReceived;
        StatusMessages = status.Messages;
        StatusBuffersSent = status.BuffersSent;
        StatusBuffersReceived = status.BuffersReceived;
    }

    [RelayCommand]
    private async Task RefreshAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;

        IsLoading = true;
        try
        {
            var status = await Task.Run(() => _mqService.GetChannelStatus(ChannelName));
            PopulateStatus(status);
            ResultText = $"Status refreshed for {ChannelName}";
            AuditLogService.Instance.Log("Channel", "Refresh", ChannelName, true);
        }
        catch (Exception ex)
        {
            ResultText = $"Refresh failed: {ex.Message}";
            AuditLogService.Instance.Log("Channel", "Refresh", ChannelName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task LoadConnInfoAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;

        IsLoading = true;
        try
        {
            var list = await Task.Run(() => _mqService.GetChannelConnInfo(ChannelName));
            ConnInfos.Clear();
            foreach (var item in list)
                ConnInfos.Add(item);
            ResultText = $"Connection info loaded for {ChannelName} ({list.Count} entries)";
        }
        catch (Exception ex)
        {
            ResultText = $"ConnInfo failed: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StartChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;

        IsLoading = true;
        try
        {
            await Task.Run(() => _mqService.StartChannel(ChannelName));
            ResultText = $"Channel {ChannelName} started";
            AuditLogService.Instance.Log("Channel", "Start", ChannelName, true);
            RefreshCommand.Execute(null);
        }
        catch (Exception ex)
        {
            ResultText = $"Start failed: {ex.Message}";
            AuditLogService.Instance.Log("Channel", "Start", ChannelName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task StopChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;

        IsLoading = true;
        try
        {
            await Task.Run(() => _mqService.StopChannel(ChannelName, IsForceStop));
            ResultText = $"Channel {ChannelName} stopped";
            AuditLogService.Instance.Log("Channel", "Stop", ChannelName, true, IsForceStop ? "force" : "");
            RefreshCommand.Execute(null);
        }
        catch (Exception ex)
        {
            ResultText = $"Stop failed: {ex.Message}";
            AuditLogService.Instance.Log("Channel", "Stop", ChannelName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ResetChannelAsync()
    {
        if (string.IsNullOrWhiteSpace(ChannelName)) return;

        IsLoading = true;
        try
        {
            await Task.Run(() => _mqService.ResetChannel(ChannelName, IsForceReset));
            ResultText = $"Channel {ChannelName} reset";
            AuditLogService.Instance.Log("Channel", "Reset", ChannelName, true, IsForceReset ? "force" : "");
            RefreshCommand.Execute(null);
        }
        catch (Exception ex)
        {
            ResultText = $"Reset failed: {ex.Message}";
            AuditLogService.Instance.Log("Channel", "Reset", ChannelName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }
}
