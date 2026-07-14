using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.i18n;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class AuditLogViewModel : ViewModelBase
{
    public ObservableCollection<AuditLogEntry> Entries { get; } = [];

    [ObservableProperty]
    private string _titleLabel = "Audit Log";

    [ObservableProperty]
    private string _saveText = "Save to File";

    [ObservableProperty]
    private string _clearText = "Clear";

    [ObservableProperty]
    private string _colTime = "Time";

    [ObservableProperty]
    private string _colCategory = "Category";

    [ObservableProperty]
    private string _colOperation = "Operation";

    [ObservableProperty]
    private string _colTarget = "Target";

    [ObservableProperty]
    private string _colSuccess = "Success";

    [ObservableProperty]
    private string _colDetails = "Details";

    public AuditLogViewModel()
    {
        AuditLogService.Instance.EntryAdded += OnEntryAdded;
        CultureHelper.Instance.CultureChanged += (_, _) => UpdateStrings();
        UpdateStrings();
    }

    private void OnEntryAdded(object? sender, AuditLogEntry entry)
    {
        Dispatcher.UIThread.Post(() => Entries.Insert(0, entry));
    }

    private void UpdateStrings()
    {
        TitleLabel = Strings.Get("AuditLog_Title");
        SaveText = Strings.Get("AuditLog_Save");
        ClearText = Strings.Get("AuditLog_Clear");
        ColTime = Strings.Get("AuditLog_ColTime");
        ColCategory = Strings.Get("AuditLog_ColCategory");
        ColOperation = Strings.Get("AuditLog_ColOperation");
        ColTarget = Strings.Get("AuditLog_ColTarget");
        ColSuccess = Strings.Get("AuditLog_ColSuccess");
        ColDetails = Strings.Get("AuditLog_ColDetails");
    }

    [RelayCommand]
    private async Task SaveToFileAsync()
    {
        var topLevel = GetTopLevel();
        if (topLevel is null) return;

        var file = await topLevel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Audit Log",
            SuggestedFileName = $"mq_audit_{DateTime.Now:yyyyMMdd_HHmmss}.csv",
            FileTypeChoices = [new FilePickerFileType("CSV") { Patterns = ["*.csv"] }]
        });

        if (file is null) return;

        var lines = new List<string> { "Timestamp,Category,Operation,Target,Success,Details" };
        foreach (var e in Entries.Reverse())
            lines.Add($"{e.Timestamp:yyyy-MM-dd HH:mm:ss},{EscapeCsv(e.Category)},{EscapeCsv(e.Operation)},{EscapeCsv(e.Target)},{e.Success},{EscapeCsv(e.Details)}");

        await using var stream = await file.OpenWriteAsync();
        await using var writer = new StreamWriter(stream, Encoding.UTF8);
        foreach (var line in lines)
            await writer.WriteLineAsync(line);

        AuditLogService.Instance.Log("System", "SaveLog", file.Name, true, $"{Entries.Count} entries");
    }

    [RelayCommand]
    private void ClearLog()
    {
        Entries.Clear();
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
}
