using Avalonia;

namespace MqManager.Desktop.i18n;

public static class Strings
{
    public static string Get(string key)
    {
        if (Application.Current?.Resources.TryGetResource(key, null, out var value) == true
            && value is string s)
            return s;
        return key;
    }

    public static string Format(string key, params object[] args)
        => string.Format(Get(key), args);
}
