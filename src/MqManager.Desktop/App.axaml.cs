using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using MqManager.Desktop.Data;
using MqManager.Desktop.i18n;
using MqManager.Desktop.ViewModels;
using MqManager.Desktop.Views;

namespace MqManager.Desktop;

public class App : Application
{
    private PreferencesRepository? _prefs;
    private TrayIcon?               _trayIcon;
    private NativeMenuItem?         _trayShowItem;
    private NativeMenuItem?         _trayExitItem;
    // True only when the user picks "Exit" from the tray menu (or OS shutdown).
    // Keeps the Closing handler from eating the event on a real quit.
    private bool _isRealExit;

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Compiled AXAML")]
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        // Load Ursa.Themes.Semi at runtime (avoids compile-time AVARES resolution)
        Styles.Add(new StyleInclude(new Uri("avares://MqManagerDesktop/"))
        {
            Source = new Uri("avares://Ursa.Themes.Semi/Index.axaml")
        });
    }

    public override void OnFrameworkInitializationCompleted()
    {
        var db = new AppDbContext();
        _prefs = new PreferencesRepository(db);

        // Register first so SetCulture below fires OnLanguageChanged if language changes
        CultureHelper.Instance.CultureChanged += OnLanguageChanged;

        // Restore the language used in the last session (defaults to "en" if never saved)
        var savedLang = _prefs.Get("language") ?? "en";
        CultureHelper.Instance.SetCulture(savedLang);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel()
            };

            SetupTrayIcon(desktop);
        }

        base.OnFrameworkInitializationCompleted();
    }

    // ── System-tray setup ────────────────────────────────────────────────────

    private void SetupTrayIcon(IClassicDesktopStyleApplicationLifetime desktop)
    {
        using var iconStream = AssetLoader.Open(
            new Uri("avares://MqManagerDesktop/Assets/app-tray.ico"));
        var windowIcon = new WindowIcon(iconStream);

        _trayShowItem = new NativeMenuItem(Strings.Get("Tray_Show"));
        _trayShowItem.Click += (_, _) => ShowMainWindow(desktop);

        _trayExitItem = new NativeMenuItem(Strings.Get("Tray_Exit"));
        _trayExitItem.Click += (_, _) =>
        {
            _isRealExit = true;
            desktop.Shutdown();
        };

        var menu = new NativeMenu();
        menu.Items.Add(_trayShowItem);
        menu.Items.Add(new NativeMenuItemSeparator());
        menu.Items.Add(_trayExitItem);

        _trayIcon = new TrayIcon
        {
            Icon        = windowIcon,
            ToolTipText = Strings.Get("App_Title"),
            Menu        = menu,
            IsVisible   = true,
        };
        _trayIcon.Clicked += (_, _) => ShowMainWindow(desktop);

        var icons = new TrayIcons { _trayIcon };
        TrayIcon.SetIcons(this, icons);

        // Intercept the title-bar close button: hide to tray instead of quitting
        if (desktop.MainWindow is { } win)
            win.Closing += OnMainWindowClosing;
    }

    private void OnMainWindowClosing(object? sender, WindowClosingEventArgs e)
    {
        // Let OS shutdown / tray "Exit" pass through; handle everything else
        if (_isRealExit) return;

        e.Cancel = true;
        if (sender is Window win)
            HandleCloseAsync(win);
    }

    private async void HandleCloseAsync(Window win)
    {
        switch (_prefs?.Get("close_behavior"))
        {
            case "minimize":
                win.Hide();
                return;

            case "exit":
                _isRealExit = true;
                win.Close();
                return;
        }

        // Preference not yet set → ask the user
        var dialog = new CloseActionDialog();
        await dialog.ShowDialog(win);

        if (dialog.SelectedAction is null) return; // Cancelled — do nothing

        if (dialog.ShouldRemember)
            _prefs?.Set("close_behavior", dialog.SelectedAction);

        if (dialog.SelectedAction == "minimize")
            win.Hide();
        else
        {
            _isRealExit = true;
            win.Close();
        }
    }

    private static void ShowMainWindow(IClassicDesktopStyleApplicationLifetime desktop)
    {
        if (desktop.MainWindow is not { } win) return;

        win.Show();
        win.Activate();
        if (win.WindowState == WindowState.Minimized)
            win.WindowState = WindowState.Normal;
    }

    // ── Language-change handler ───────────────────────────────────────────────

    [UnconditionalSuppressMessage("Trimming", "IL2026",
        Justification = "Runtime ResourceInclude for i18n language switching; Avalonia AXAML resources are trimming-safe")]
    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var isZh = CultureHelper.Instance.CurrentCulture.TwoLetterISOLanguageName == "zh";

        // Persist the language choice so the next launch restores it
        _prefs?.Set("language", isZh ? "zh" : "en");

        var source = new Uri(isZh
            ? "avares://MqManagerDesktop/i18n/Strings.zh.axaml"
            : "avares://MqManagerDesktop/i18n/Strings.axaml");

        if (Resources is ResourceDictionary dict)
        {
            dict.MergedDictionaries.Clear();
            dict.MergedDictionaries.Add(new ResourceInclude(source) { Source = source });
        }

        // Refresh tray-icon labels after the new resource dictionary is active
        if (_trayShowItem is not null) _trayShowItem.Header = Strings.Get("Tray_Show");
        if (_trayExitItem is not null) _trayExitItem.Header = Strings.Get("Tray_Exit");
        if (_trayIcon    is not null) _trayIcon.ToolTipText = Strings.Get("App_Title");
    }
}
