using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace MqManager.Desktop.Converters;

public static class ValueConverters
{
    public static readonly IValueConverter BoolToVisible =
        new FuncValueConverter<bool, bool>(value => value);

    public static readonly IValueConverter InverseBool =
        new FuncValueConverter<bool, bool>(value => !value);

    public static readonly IValueConverter NullToVisible =
        new FuncValueConverter<object?, bool>(value => value is not null);

    public static readonly IValueConverter StatusToBrush =
        new FuncValueConverter<string?, IBrush>(status =>
        {
            return status?.ToLowerInvariant() switch
            {
                "running" => new SolidColorBrush(Color.Parse("#388E3C")),
                "stopped" => new SolidColorBrush(Color.Parse("#D32F2F")),
                "starting" => new SolidColorBrush(Color.Parse("#F57C00")),
                "stopping" => new SolidColorBrush(Color.Parse("#F57C00")),
                _ => new SolidColorBrush(Color.Parse("#757575")),
            };
        });
}
