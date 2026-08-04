using System.Globalization;
using TraderUI.Models;

namespace TraderUI.Converters;

// ==================== BOOL CONVERTERS ====================
public class InvertBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? !b : false;
}

public class BoolToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Color.FromArgb("#00E676") : Color.FromArgb("#FF3D57");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToStarConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "★" : "☆";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== NULL CONVERTERS ====================
public class NotNullConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToFalseConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value != null;
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== SIGNAL / DIRECTION CONVERTERS ====================
public class DirectionToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SignalDirection dir)
            return dir == SignalDirection.Buy ? Color.FromArgb("#00E676") : dir == SignalDirection.Sell ? Color.FromArgb("#FF3D57") : Color.FromArgb("#A0AEC0");
        return Color.FromArgb("#A0AEC0");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class TradeTypeColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is TradeType t)
            return t == TradeType.Buy ? Color.FromArgb("#00E676") : Color.FromArgb("#FF3D57");
        return Color.FromArgb("#A0AEC0");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== BOT STATUS CONVERTERS ====================
public class BotStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Color.FromArgb("#1A3D2B") : Color.FromArgb("#1A1A2E");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BotDotColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? Color.FromArgb("#00E676") : Color.FromArgb("#A0AEC0");
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BotStatusTextConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && b ? "RUNNING" : "STOPPED";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== PNL CONVERTERS ====================
public class PnlColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal d) return d >= 0 ? Color.FromArgb("#00E676") : Color.FromArgb("#FF3D57");
        if (value is double dbl) return dbl >= 0 ? Color.FromArgb("#00E676") : Color.FromArgb("#FF3D57");
        return Color.FromArgb("#A0AEC0");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== CONFIDENCE CONVERTERS ====================
public class ConfidenceToColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is ConfidenceLevel cl)
            return cl switch
            {
                ConfidenceLevel.VeryHigh => Color.FromArgb("#00E676"),
                ConfidenceLevel.High => Color.FromArgb("#69F0AE"),
                ConfidenceLevel.Medium => Color.FromArgb("#FFD740"),
                _ => Color.FromArgb("#A0AEC0")
            };
        return Color.FromArgb("#A0AEC0");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== SIGNAL STATUS CONVERTERS ====================
public class SignalStatusColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SignalStatus s)
            return s switch
            {
                SignalStatus.Won => Color.FromArgb("#00E676"),
                SignalStatus.Lost => Color.FromArgb("#FF3D57"),
                SignalStatus.Live => Color.FromArgb("#00D4FF"),
                _ => Color.FromArgb("#A0AEC0")
            };
        return Color.FromArgb("#A0AEC0");
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

// ==================== NUMERIC CONVERTERS ====================
public class DoubleToPercentConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is double d ? $"{d:F1}%" : "0%";
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class DecimalToStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        string fmt = parameter as string ?? "F5";
        return value is decimal d ? d.ToString(fmt) : "0";
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => decimal.TryParse(value?.ToString(), out var d) ? d : 0m;
}

// ==================== EMPTY COLLECTION CONVERTER ====================
public class EmptyCollectionToBoolConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is System.Collections.ICollection col) return col.Count == 0;
        return true;
    }
    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
