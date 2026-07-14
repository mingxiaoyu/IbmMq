using System;
using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Platform;
using MqManager.Desktop.i18n;
using MqManager.Desktop.ViewModels;
using MqManager.Desktop.Views;

namespace MqManager.Desktop;

public class App : Application
{
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
        CultureHelper.Instance.CultureChanged += OnLanguageChanged;

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
