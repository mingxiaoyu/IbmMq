using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class ConnectionListViewModel : ViewModelBase
{
    private readonly IMqService _mqService;
    private List<ConnectionInfo> _cachedConnections = [];
    private List<ConnectionInfo> _allConnections = [];

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private ConnectionInfo? _selectedConnection;

    [ObservableProperty]
    private bool _isLoading;

    // Translated strings
    [ObservableProperty]
    private string _filterPlaceholder = "Filter connections...";

    [ObservableProperty]
    private string _exportCsvText = "Export CSV";

    [ObservableProperty]
    private string _titleLabel = "Connections";

    [ObservableProperty]
    private string _refreshText = "Refresh";

    [ObservableProperty]
    private string _appLabel = "App:";

    [ObservableProperty]
    private string _loadingText = "Loading...";

    public ObservableCollection<ConnectionInfo> Connections { get; } = [];

    public event Action<ConnectionInfo>? ConnectionSelected;

    public ConnectionListViewModel(IMqService mqService)
    {
        _mqService = mqService;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        FilterPlaceholder = Strings.Get("Common_Filter");
        ExportCsvText = Strings.Get("Connection_ExportCsv");
        TitleLabel = Strings.Get("Sidebar_Connections");
        RefreshText = Strings.Get("Common_Refresh");
        AppLabel = Strings.Get("Connection_AppLabel");
        LoadingText = Strings.Get("Common_Loading");
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public void InvalidateCache()
    {
        _cachedConnections.Clear();
    }

    [RelayCommand]
    private async Task LoadConnectionsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // Use in-memory cache if available and no filter
            if (_cachedConnections.Count > 0 && string.IsNullOrWhiteSpace(Filter))
            {
                _allConnections = _cachedConnections;
                Connections.Clear();
                foreach (var conn in _cachedConnections)
                    Connections.Add(conn);
            }

            // Load fresh from CLI
            try
            {
                var fresh = await Task.Run(() => _mqService.ListConnections(
                    string.IsNullOrWhiteSpace(Filter) ? null : Filter));

                _allConnections = fresh;
                Connections.Clear();
                foreach (var conn in fresh)
                    Connections.Add(conn);

                // Update in-memory cache
                if (string.IsNullOrWhiteSpace(Filter))
                    _cachedConnections = fresh;
            }
            catch (Exception)
            {
                if (Connections.Count == 0)
                    throw;
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task ExportCsvAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export Connections",
            SuggestedFileName = $"mq_connections_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }]
        });

        if (file is null) return;

        var lines = new List<string> { "ChannelName,ConnectionName,AppName,UserId,Status" };
        foreach (var conn in _allConnections)
            lines.Add($"{EscapeCsv(conn.ChannelName)},{EscapeCsv(conn.ConnectionName)},{EscapeCsv(conn.AppName)},{EscapeCsv(conn.UserId)},{EscapeCsv(conn.Status)}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        foreach (var line in lines)
            await writer.WriteLineAsync(line);

        AuditLogService.Instance.Log("Connection", "ExportCSV", file.Name, true, $"{_allConnections.Count} rows");
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static TopLevel? GetTopLevel()
    {
        if (App.Current?.ApplicationLifetime is
            Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
            return TopLevel.GetTopLevel(desktop.MainWindow);
        return null;
    }

    partial void OnSelectedConnectionChanged(ConnectionInfo? value)
    {
        if (value is not null)
            ConnectionSelected?.Invoke(value);
    }
}
