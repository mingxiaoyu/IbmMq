using System;
using System.Globalization;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace MqManager.Desktop.i18n;

public partial class CultureHelper : ObservableObject
{
    private static readonly Lazy<CultureHelper> _instance = new(() => new CultureHelper());
    public static CultureHelper Instance => _instance.Value;

    [ObservableProperty]
    private CultureInfo _currentCulture = CultureInfo.GetCultureInfo("en");

    public event EventHandler? CultureChanged;

    private CultureHelper()
    {
    }

    public void SetCulture(string cultureName)
    {
        var culture = CultureInfo.GetCultureInfo(cultureName);
        if (CurrentCulture.Name != culture.Name)
        {
            CurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentCulture = culture;
            CultureInfo.DefaultThreadCurrentUICulture = culture;
            Thread.CurrentThread.CurrentCulture = culture;
            Thread.CurrentThread.CurrentUICulture = culture;
            CultureChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public void ToggleLanguage()
    {
        SetCulture(CurrentCulture.Name == "zh" ? "en" : "zh");
    }
}
