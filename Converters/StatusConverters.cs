using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using EquipmentStatusTracker.WPF.Models;

namespace EquipmentStatusTracker.WPF.Converters;

public class StatusToColorConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentStatus status)
        {
            return status switch
            {
                EquipmentStatus.Normal => new SolidColorBrush(Color.FromRgb(63, 185, 80)),
                EquipmentStatus.Abnormal => new SolidColorBrush(Color.FromRgb(248, 81, 73)),
                EquipmentStatus.Warning => new SolidColorBrush(Color.FromRgb(210, 153, 34)),
                _ => new SolidColorBrush(Color.FromRgb(139, 148, 158))
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentStatus status)
        {
            return status switch
            {
                EquipmentStatus.Normal => new SolidColorBrush(Color.FromArgb(38, 63, 185, 80)),
                EquipmentStatus.Abnormal => new SolidColorBrush(Color.FromArgb(38, 248, 81, 73)),
                EquipmentStatus.Warning => new SolidColorBrush(Color.FromArgb(38, 210, 153, 34)),
                _ => new SolidColorBrush(Color.FromArgb(38, 139, 148, 158))
            };
        }
        return new SolidColorBrush(Colors.Transparent);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            var invert = parameter?.ToString() == "Invert";
            return (b ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class FilterToBoolConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var currentValue = value?.ToString() ?? "";
        var filterValue = parameter?.ToString() ?? "";
        return currentValue == filterValue;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class EquipmentTypeToIconConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is EquipmentType type)
        {
            return type switch
            {
                EquipmentType.Valve => "✕",
                EquipmentType.Breaker => "▭",
                EquipmentType.Pump => "◯",
                EquipmentType.Chiller => "❄",
                EquipmentType.Generator => "⚡",
                EquipmentType.ATS => "⇄",
                EquipmentType.UPS => "🔋",
                EquipmentType.Motor => "⚙",
                EquipmentType.Transformer => "◎",
                EquipmentType.Switch => "◉",
                EquipmentType.PDU => "▦",
                _ => "●"
            };
        }
        return "●";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ZoomToPercentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double zoom)
        {
            return $"{(int)(zoom * 100)}%";
        }
        return "100%";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringToUpperConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        return value?.ToString()?.ToUpper() ?? string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class StringEqualityConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length >= 2 && values[0] is string str1 && values[1] is string str2)
        {
            return string.Equals(str1, str2, StringComparison.OrdinalIgnoreCase);
        }
        return false;
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var invert = parameter?.ToString() == "Invert";
        var isNullOrEmpty = value == null || (value is string str && string.IsNullOrEmpty(str));
        return (isNullOrEmpty ^ invert) ? Visibility.Collapsed : Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GreaterThanZeroConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is int count)
        {
            return count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }
        return Visibility.Collapsed;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class ConnectionStrokeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is string colorStr)
        {
            try
            {
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorStr));
            }
            catch
            {
                return new SolidColorBrush(Colors.Gray);
            }
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GridSizeToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int gridSize = 20;
        if (value is int gs) gridSize = gs;

        var drawingGroup = new DrawingGroup();
        
        // Background
        drawingGroup.Children.Add(new GeometryDrawing(
            new SolidColorBrush(Color.FromRgb(10, 14, 20)),
            null,
            new RectangleGeometry(new Rect(0, 0, gridSize, gridSize))));
        
        // Grid lines
        var lineGeometry = new GeometryGroup();
        lineGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(gridSize, 0)));
        lineGeometry.Children.Add(new LineGeometry(new Point(0, 0), new Point(0, gridSize)));
        
        drawingGroup.Children.Add(new GeometryDrawing(
            null,
            new Pen(new SolidColorBrush(Color.FromArgb(48, 96, 108, 128)), 1),
            lineGeometry));

        return drawingGroup;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class HalfValueConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
        {
            return d / 2;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GridViewportConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        int gridSize = 20;
        if (value is int gs) gridSize = gs;

        return new Rect(0, 0, gridSize, gridSize);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

public class GridViewportMultiConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        int gridSize = 20;
        double panX = 0;
        double panY = 0;
        double zoomLevel = 1;

        if (values.Length >= 1 && values[0] is int gs) gridSize = gs;
        if (values.Length >= 2 && values[1] is double px) panX = px;
        if (values.Length >= 3 && values[2] is double py) panY = py;
        if (values.Length >= 4 && values[3] is double zl) zoomLevel = zl;

        // Calculate offset to make grid appear infinite
        // The grid pattern needs to shift opposite to pan direction, scaled by zoom
        double scaledGridSize = gridSize * zoomLevel;
        double offsetX = panX % scaledGridSize;
        double offsetY = panY % scaledGridSize;

        return new Rect(offsetX, offsetY, scaledGridSize, scaledGridSize);
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
