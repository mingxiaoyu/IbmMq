using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class QueueActionsViewModel : ViewModelBase
{
    private readonly IMqService _mqService;
    private Action? _refreshAction;

    [ObservableProperty]
    private bool _isCreating;

    [ObservableProperty]
    private string _newQueueName = string.Empty;

    [ObservableProperty]
    private string _newQueueType = "local";

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public QueueActionsViewModel(IMqService mqService)
    {
        _mqService = mqService;
    }

    public void SetRefreshAction(Action refresh)
    {
        _refreshAction = refresh;
    }

    [RelayCommand]
    private void StartCreate()
    {
        NewQueueName = string.Empty;
        NewQueueType = "local";
        IsCreating = true;
    }

    [RelayCommand]
    private async Task CreateQueueAsync()
    {
        if (string.IsNullOrWhiteSpace(NewQueueName)) return;

        var name = NewQueueName;
        try
        {
            await Task.Run(() => _mqService.CreateQueue(name, NewQueueType));
            StatusMessage = string.Empty;
            IsCreating = false;
            _refreshAction?.Invoke();
            AuditLogService.Instance.Log("Queue", "Create", name, true, NewQueueType);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Create failed: {ex.Message}";
            AuditLogService.Instance.Log("Queue", "Create", name, false, ex.Message);
        }
    }

    [RelayCommand]
    private void CancelCreate()
    {
        IsCreating = false;
        StatusMessage = string.Empty;
    }

    [RelayCommand]
    private async Task DeleteQueueAsync(string? queueName)
    {
        if (string.IsNullOrEmpty(queueName)) return;

        try
        {
            await Task.Run(() => _mqService.DeleteQueue(queueName, force: true));
            _refreshAction?.Invoke();
            AuditLogService.Instance.Log("Queue", "Delete", queueName, true);
        }
        catch (Exception ex)
        {
            StatusMessage = $"Delete failed: {ex.Message}";
            AuditLogService.Instance.Log("Queue", "Delete", queueName, false, ex.Message);
        }
    }
}
