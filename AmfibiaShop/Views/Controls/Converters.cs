using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace AmfibiaShop.Views.Controls;

/// <summary>«#RRGGBB» → кисть.</summary>
public class HexBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        value is string hex && hex.Length > 0 ? new SolidColorBrush(ProductArt.ToColor(hex)) : Brushes.Transparent;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>Два цвета палитры бренда → диагональный градиент.</summary>
public class GradientConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        string a = values.Length > 0 && values[0] is string s0 ? s0 : "#111112";
        string b = values.Length > 1 && values[1] is string s1 ? s1 : "#1D1D20";
        return new LinearGradientBrush(ProductArt.ToColor(a), ProductArt.ToColor(b), new Point(0, 0), new Point(1, 1));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>Ключ шрифта бренда (serif, gothic, grotesk…) → шрифт.</summary>
public class BrandFontConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        Application.Current?.TryFindResource("BrandFont." + value) as FontFamily
        ?? Application.Current?.TryFindResource("FontDisplayBold") as FontFamily
        ?? new FontFamily("Georgia");

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>null или пустая строка → скрыть, иначе показать. С параметром «invert» — наоборот.</summary>
public class EmptyToCollapsedConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool empty = value == null || value is string s && s.Length == 0 || value is int n && n == 0;
        if (parameter as string == "invert") empty = !empty;
        return empty ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}

/// <summary>bool → видимость. С параметром «invert» — наоборот.</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        bool flag = value is true;
        if (parameter as string == "invert") flag = !flag;
        return flag ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => Binding.DoNothing;
}
