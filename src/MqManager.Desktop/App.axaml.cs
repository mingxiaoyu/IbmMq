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
        }

        base.OnFrameworkInitializationCompleted();
    }

    [UnconditionalSuppressMessage("Trimming", "IL2026", Justification = "Runtime ResourceInclude for i18n language switching; Avalonia AXAML resources are trimming-safe")]
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
    }
}
