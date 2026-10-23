using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;
using AuditLog = MqManager.Desktop.Services.AuditLogService;

namespace MqManager.Desktop.ViewModels;

public partial class MessagePanelViewModel : ViewModelBase
{
    private readonly IMqService _mqService;

    [ObservableProperty]
    private string _queueName = string.Empty;

    [ObservableProperty]
    private long _queueDepth;

    [ObservableProperty]
    private int _selectedMode;

    public bool IsTextMode => SelectedMode == 0;
    public bool IsFileMode => SelectedMode == 1;
    public bool IsFolderMode => SelectedMode == 2;

    partial void OnSelectedModeChanged(int value)
    {
        OnPropertyChanged(nameof(IsTextMode));
        OnPropertyChanged(nameof(IsFileMode));
        OnPropertyChanged(nameof(IsFolderMode));
    }

    [RelayCommand]
    private void SetTextMode() => SelectedMode = 0;

    [RelayCommand]
    private void SetFileMode() => SelectedMode = 1;

    [RelayCommand]
    private void SetFolderMode() => SelectedMode = 2;

    [ObservableProperty]
    private string _messageText = string.Empty;

    [ObservableProperty]
    private string _filePath = string.Empty;

    [ObservableProperty]
    private string _folderPath = string.Empty;

    [ObservableProperty]
    private string _filePathDisplay = string.Empty;

    [ObservableProperty]
    private string _folderPathDisplay = string.Empty;

    [ObservableProperty]
    private int _messageCount = 1;

    public string[] MessageCountSuggestions { get; } = ["1", "5", "10", "20", "50", "100", "200", "500", "1000"];

    [ObservableProperty]
    private string _messageCountText = "1";

    partial void OnMessageCountTextChanged(string value)
    {
        if (int.TryParse(value, out var count) && count >= 1 && count <= 10000)
            MessageCount = count;
    }

    [ObservableProperty]
    private bool _isLoading;

    // Translated strings
    [ObservableProperty]
    private string _depthLabel = "Depth";

    [ObservableProperty]
    private string _depthPrefix = "Depth: {0}";

    public string DepthDisplay => string.Format(DepthPrefix, QueueDepth);

    [ObservableProperty]
    private string _singleMessageText = "Text Message";

    [ObservableProperty]
    private string _fromFileText = "File";

    [ObservableProperty]
    private string _fromFolderText = "Folder";

    [ObservableProperty]
    private string _enterMessageText = "Enter message text...";

    [ObservableProperty]
    private string _countLabel = "Count:";

    [ObservableProperty]
    private string _sendText = "Send";

    [ObservableProperty]
    private string _browseText = "Browse";

    [ObservableProperty]
    private string _getMessageText = "Get";

    [ObservableProperty]
    private string _purgeText = "Purge";

    [ObservableProperty]
    private string _consumeAllText = "Consume All";

    [ObservableProperty]
    private string _loadingText = "Loading...";

    [ObservableProperty]
    private string _idLabel = "ID:";

    [ObservableProperty]
    private string _correlationLabel = "Corr:";

    [ObservableProperty]
    private string _selectFileText = "Select File";

    [ObservableProperty]
    private string _selectFolderText = "Select Folder";

    [ObservableProperty]
    private string _browseFileText = "Browse...";

    [ObservableProperty]
    private string _browseFolderText = "Browse...";

    [ObservableProperty]
    private string _browseButton = "Browse";

    [ObservableProperty]
    private string _exportAllText = "Export All";

    [ObservableProperty]
    private string _copyBodyLabel = "Copy Body";

    public ObservableCollection<MessageInfo> Messages { get; } = [];

    public MessagePanelViewModel(IMqService mqService)
    {
        _mqService = mqService;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        DepthLabel = Strings.Get("Msg_Depth");
        DepthPrefix = Strings.Get("Msg_DepthPrefix");
        SingleMessageText = Strings.Get("Msg_ModeSingle");
        FromFileText = Strings.Get("Msg_ModeFile");
        FromFolderText = Strings.Get("Msg_ModeFolder");
        EnterMessageText = Strings.Get("Msg_EnterMessage");
        CountLabel = Strings.Get("Msg_CountLabel");
        SendText = Strings.Get("Msg_Send");
        BrowseText = Strings.Get("Msg_Browse");
        GetMessageText = Strings.Get("Msg_Get");
        PurgeText = Strings.Get("Msg_Purge");
        ConsumeAllText = Strings.Get("Msg_ConsumeAll");
        LoadingText = Strings.Get("Common_Loading");
        IdLabel = Strings.Get("Msg_IdLabel");
        CorrelationLabel = Strings.Get("Msg_CorrLabel");
        SelectFileText = Strings.Get("Msg_SelectFile");
        SelectFolderText = Strings.Get("Msg_SelectFolder");
        BrowseFileText = Strings.Get("Msg_BrowseBtn");
        BrowseFolderText = Strings.Get("Msg_BrowseBtn");
        BrowseButton = Strings.Get("Msg_Browse");
        ExportAllText = Strings.Get("Msg_ExportAll");
        CopyBodyLabel = Strings.Get("Msg_CopyBody");
    }

    partial void OnQueueDepthChanged(long value)
    {
        OnPropertyChanged(nameof(DepthDisplay));
    }

    public void SetQueue(QueueInfo queue)
    {
        QueueName = queue.Name;
        QueueDepth = queue.Depth;
        Messages.Clear();
        MessageText = string.Empty;
        FilePath = string.Empty;
        FolderPath = string.Empty;
        FilePathDisplay = string.Empty;
        FolderPathDisplay = string.Empty;
        LoadDepth();
    }

    private void LoadDepth()
    {
        try
        {
            QueueDepth = _mqService.GetQueueDepth(QueueName);
        }
        catch
        {
            // depth unavailable, keep current value
        }
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        IsLoading = true;
        try
        {
            await Task.Run(() =>
            {
                switch (SelectedMode)
                {
                    case 0: // single text
                        if (!string.IsNullOrEmpty(MessageText))
                            _mqService.PutMessage(QueueName, MessageText);
                        break;
                    case 1: // file
                        if (!string.IsNullOrEmpty(FilePath) && File.Exists(FilePath))
                            _mqService.PutMessageFromFile(QueueName, FilePath);
                        break;
                    case 2: // folder
                        if (!string.IsNullOrEmpty(FolderPath) && Directory.Exists(FolderPath))
                        {
                            foreach (var file in Directory.GetFiles(FolderPath))
                                _mqService.PutMessageFromFile(QueueName, file);
                        }
                        break;
                }
            });

            AuditLog.Instance.Log("Message", "Put", QueueName, true);
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "Put", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task BrowseMessagesAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        IsLoading = true;
        try
        {
            var messages = await Task.Run(() => _mqService.BrowseMessages(QueueName, MessageCount));
            Messages.Clear();
            foreach (var msg in messages)
                Messages.Add(msg);

            AuditLog.Instance.Log("Message", "Browse", QueueName, true, $"{messages.Count} messages");
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "Browse", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task GetMessagesAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        IsLoading = true;
        try
        {
            var messages = await Task.Run(() => _mqService.GetMessages(QueueName, MessageCount));
            Messages.Clear();
            foreach (var msg in messages)
                Messages.Add(msg);

            AuditLog.Instance.Log("Message", "Get", QueueName, true, $"{messages.Count} messages");
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "Get", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task PurgeQueueAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        IsLoading = true;
        try
        {
            await Task.Run(() => _mqService.PurgeQueue(QueueName, force: true));
            AuditLog.Instance.Log("Message", "Purge", QueueName, true);
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "Purge", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ConsumeAllAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        IsLoading = true;
        var totalConsumed = 0;
        try
        {
            while (true)
            {
                var messages = await Task.Run(() => _mqService.GetMessages(QueueName, 100));
                if (messages.Count == 0) break;

                totalConsumed += messages.Count;
                foreach (var msg in messages)
                    Messages.Add(msg);
            }

            AuditLog.Instance.Log("Message", "ConsumeAll", QueueName, true, $"{totalConsumed} messages");
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "ConsumeAll", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportAllMessagesAsync()
    {
        if (string.IsNullOrWhiteSpace(QueueName)) return;

        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = $"Export messages from {QueueName}",
            AllowMultiple = false
        });

        if (folders.Count == 0) return;

        var folderPath = folders[0].TryGetLocalPath() ?? folders[0].Path.LocalPath;

        IsLoading = true;
        try
        {
            var messages = await Task.Run(() => _mqService.BrowseMessages(QueueName, int.MaxValue));

            await Task.Run(() =>
            {
                for (var i = 0; i < messages.Count; i++)
                {
                    var body = messages[i].Body ?? string.Empty;
                    var ext = DetectExtension(body);
                    var fileName = BuildExportFileName(QueueName, messages[i].PutDateTime, i + 1, ext);
                    var filePath = Path.Combine(folderPath, fileName);
                    File.WriteAllText(filePath, body, System.Text.Encoding.UTF8);
                }
            });

            Messages.Clear();
            foreach (var msg in messages)
                Messages.Add(msg);

            AuditLog.Instance.Log("Message", "ExportAll", QueueName, true, $"{messages.Count} messages to {folderPath}");
            LoadDepth();
        }
        catch (Exception ex)
        {
            AuditLog.Instance.Log("Message", "ExportAll", QueueName, false, ex.Message);
        }
        finally
        {
            IsLoading = false;
        }
    }

    private static string BuildExportFileName(string queueName, string? putDateTime, int index, string ext)
    {
        var dt = string.IsNullOrEmpty(putDateTime)
            ? "0000-00-00_00_00_00"
            : putDateTime.Replace(' ', '_').Replace(':', '_');
        return $"{queueName}_{dt}_{index:D6}{ext}";
    }

    private static string DetectExtension(string body)
    {
        if (string.IsNullOrWhiteSpace(body)) return ".txt";

        var text = body.TrimStart();
        if (text.Length > 0 && text[0] == '\uFEFF')
            text = text[1..].TrimStart();

        if (text.Length == 0) return ".txt";

        // Try JSON (fast: fails immediately on non-JSON first char)
        try
        {
            using var _ = System.Text.Json.JsonDocument.Parse(text);
            return ".json";
        }
        catch { }

        // Detect XML: try single-root parse first, then fall back to wrapped parse
        if (text.StartsWith('<'))
        {
            try
            {
                System.Xml.Linq.XDocument.Parse(text);
                return ".xml";
            }
            catch { }

            try
            {
                System.Xml.Linq.XDocument.Parse($"<root>{text}</root>");
                return ".xml";
            }
            catch { }
        }

        return ".txt";
    }

    [RelayCommand]
    private async Task CopyMessageBodyAsync(string? body)
    {
        if (string.IsNullOrEmpty(body)) return;
        var topLevel = GetTopLevel();
        if (topLevel?.Clipboard is null) return;
        await topLevel.Clipboard.SetTextAsync(body);
    }

    private static TopLevel? GetTopLevel()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return TopLevel.GetTopLevel(desktop.MainWindow);
        return null;
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var files = await topLevel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select File",
            AllowMultiple = false
        });

        if (files.Count > 0)
            FilePath = files[0].TryGetLocalPath() ?? files[0].Path.ToString();
    }

    [RelayCommand]
    private async Task BrowseFolderAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
            FolderPath = folders[0].TryGetLocalPath() ?? folders[0].Path.ToString();
    }
}
