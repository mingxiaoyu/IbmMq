using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.Data;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class QueueListViewModel : ViewModelBase
{
    private readonly IMqService _mqService;
    private readonly CacheRepository _cacheRepo;
    private ServerConfig? _currentServer;
    private List<QueueInfo> _allQueues = [];

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private QueueInfo? _selectedQueue;

    [ObservableProperty]
    private bool _sortAscending = true;

    [ObservableProperty]
    private bool _isLoading;

    // Translated strings
    [ObservableProperty]
    private string _filterPlaceholder = "Filter queues...";

    [ObservableProperty]
    private string _titleLabel = "Queues";

    [ObservableProperty]
    private string _refreshText = "Refresh";

    [ObservableProperty]
    private string _loadingText = "Loading...";

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private string _countText = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public ObservableCollection<QueueInfo> Queues { get; } = [];

    public event Action<QueueInfo>? QueueSelected;

    public QueueListViewModel(IMqService mqService, CacheRepository cacheRepo)
    {
        _mqService = mqService;
        _cacheRepo = cacheRepo;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    public void SetServer(ServerConfig config)
    {
        _currentServer = config;
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        FilterPlaceholder = Strings.Get("Common_Filter");
        TitleLabel = Strings.Get("Sidebar_Queues");
        RefreshText = Strings.Get("Common_Refresh");
        LoadingText = Strings.Get("Common_Loading");
    }

    [RelayCommand]
    private async Task LoadQueuesAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // Load from cache first
            if (_currentServer is not null)
            {
                var cached = _cacheRepo.GetCachedQueues(_currentServer);
                if (cached.Count > 0)
                {
                    _allQueues = cached;
                    ApplyFilter();
                }
            }

            // Try loading from CLI in background
            try
            {
                var fresh = await Task.Run(() => _mqService.ListQueues());
                _allQueues = fresh;
                ApplyFilter();

                // Update cache with fresh data
                if (_currentServer is not null)
                    _cacheRepo.SaveQueues(_currentServer, fresh);
            }
            catch (Exception)
            {
                // If CLI fails, we still have cache data; only show error if no cache
                if (_allQueues.Count == 0)
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
    private async Task RefreshAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // Clear cache, then do fresh load
            if (_currentServer is not null)
                _cacheRepo.ClearCache(_currentServer);

            var fresh = await Task.Run(() => _mqService.ListQueues());
            _allQueues = fresh;
            ApplyFilter();

            // Save to cache
            if (_currentServer is not null)
                _cacheRepo.SaveQueues(_currentServer, fresh);
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
    private void ToggleSort()
    {
        SortAscending = !SortAscending;
        ApplyFilter();
    }

    partial void OnFilterChanged(string value)
    {
        ApplyFilter();
    }

    partial void OnErrorMessageChanged(string value)
    {
        OnPropertyChanged(nameof(HasError));
    }

    partial void OnSelectedQueueChanged(QueueInfo? value)
    {
        if (value is not null)
            QueueSelected?.Invoke(value);
    }

    private void ApplyFilter()
    {
        Queues.Clear();

        var filtered = string.IsNullOrWhiteSpace(Filter)
            ? _allQueues
            : _allQueues.Where(q => GlobMatch(q.Name, Filter)).ToList();

        var sorted = SortAscending
            ? filtered.OrderBy(q => q.Name).ThenBy(q => q.Depth)
            : filtered.OrderByDescending(q => q.Name).ThenByDescending(q => q.Depth);

        foreach (var q in sorted)
            Queues.Add(q);

        CountText = Strings.Format("Queue_CountText", Queues.Count, _allQueues.Count);
    }

    private static bool GlobMatch(string name, string pattern)
    {
        // Full fuzzy: treat the input as a glob that can match anywhere in the name.
        // If user types plain text (no * or ?), wrap with implicit * on both sides.
        var hasWildcard = pattern.Contains('*') || pattern.Contains('?');
        var p = hasWildcard ? pattern : $"*{pattern}*";

        var regexPattern = "^" + Regex.Escape(p)
            .Replace("\\*", ".*")
            .Replace("\\?", ".") + "$";
        return Regex.IsMatch(name, regexPattern, RegexOptions.IgnoreCase);
    }
}
