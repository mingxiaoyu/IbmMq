using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MqManager.Desktop.i18n;
using MqManager.Core.Models;
using MqManager.Desktop.Models;
using MqManager.Desktop.Services;

namespace MqManager.Desktop.ViewModels;

public partial class ChannelListViewModel : ViewModelBase
{
    private readonly IMqService _mqService;
    private List<ChannelInfo> _cachedChannels = [];

    [ObservableProperty]
    private string _filter = string.Empty;

    [ObservableProperty]
    private ChannelInfo? _selectedChannel;

    [ObservableProperty]
    private bool _isLoading;

    // Translated strings
    [ObservableProperty]
    private string _filterPlaceholder = "Filter channels...";

    [ObservableProperty]
    private string _titleLabel = "Channels";

    [ObservableProperty]
    private string _refreshText = "Refresh";

    [ObservableProperty]
    private string _loadingText = "Loading...";

    public ObservableCollection<ChannelInfo> Channels { get; } = [];

    public event Action<ChannelInfo>? ChannelSelected;

    public ChannelListViewModel(IMqService mqService)
    {
        _mqService = mqService;
        CultureHelper.Instance.CultureChanged += (_, _) => OnCultureChanged();
        UpdateStrings();
    }

    private void OnCultureChanged() => UpdateStrings();

    private void UpdateStrings()
    {
        FilterPlaceholder = Strings.Get("Common_Filter");
        TitleLabel = Strings.Get("Sidebar_Channels");
        RefreshText = Strings.Get("Common_Refresh");
        LoadingText = Strings.Get("Common_Loading");
    }

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    public bool HasError => !string.IsNullOrEmpty(ErrorMessage);

    public void InvalidateCache()
    {
        _cachedChannels.Clear();
    }

    [RelayCommand]
    private async Task LoadChannelsAsync()
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            // Use in-memory cache if available and no filter
            if (_cachedChannels.Count > 0 && string.IsNullOrWhiteSpace(Filter))
            {
                Channels.Clear();
                foreach (var ch in _cachedChannels)
                    Channels.Add(ch);
            }

            // Load fresh from CLI
            try
            {
                var fresh = await Task.Run(() => _mqService.ListChannels(
                    string.IsNullOrWhiteSpace(Filter) ? null : Filter));

                Channels.Clear();
                foreach (var ch in fresh)
                    Channels.Add(ch);

                // Update in-memory cache
                if (string.IsNullOrWhiteSpace(Filter))
                    _cachedChannels = fresh;
            }
            catch (Exception)
            {
                if (Channels.Count == 0)
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

    partial void OnSelectedChannelChanged(ChannelInfo? value)
    {
        if (value is not null)
            ChannelSelected?.Invoke(value);
    }
}
