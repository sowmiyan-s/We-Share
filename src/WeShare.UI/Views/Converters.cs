using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace WeShare.UI.Views
{
    public class ProgressToWidthConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double progress && double.TryParse(parameter?.ToString(), out double maxWidth))
            {
                return progress / 100.0 * maxWidth;
            }
            return 0.0;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class FileSizeConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is long size)
            {
                string[] sizes = { "B", "KB", "MB", "GB", "TB" };
                int order = 0;
                double len = size;
                while (len >= 1024 && order < sizes.Length - 1)
                {
                    order++;
                    len /= 1024;
                }
                return $"{len:0.##} {sizes[order]}";
            }
            return "0 B";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class RadarPositionConverter : IMultiValueConverter
    {
        public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count < 3 || values[0] is not WeShare.Core.Models.DeviceModel device || values[1] is not System.Collections.ObjectModel.ObservableCollection<WeShare.Core.Models.DeviceModel> devices || values[2] is not string type) 
                return 0.0;
            
            int index = devices.IndexOf(device);
            if (index < 0) return 0.0;

            // Put them on different rings based on index (match XAML rings: 90, 210, 260)
            double[] rings = { 110, 190, 240 }; 
            double radius = rings[index % rings.Length];
            
            // Offset angles so they don't overlap (spread them out)
            double angle = (index * 60 + (index / 3) * 20) * (Math.PI / 180.0);
            
            // Canvas Center (520x520 radar panel -> 260x260 center)
            double centerX = 260;
            double centerY = 260;

            // Icon size offset (64x64 icon -> subtract 32)
            if (type == "X") return centerX + Math.Cos(angle) * radius - 32;
            return centerY + Math.Sin(angle) * radius - 32;
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public class DeviceTypeToIconConverter : IValueConverter
    {
        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string name)
            {
                string lower = name.ToLowerInvariant();
                if (lower.Contains("iphone") || lower.Contains("ipad") || lower.Contains("ios"))
                    return "📱";
                if (lower.Contains("android"))
                    return "📱";
                if (lower.Contains("mac") || lower.Contains("os x") || lower.Contains("osx"))
                    return "💻";
                if (lower.Contains("linux"))
                    return "🐧";
                if (lower.Contains("windows") || lower.Contains("win"))
                    return "💻";
                if (lower.Contains("web"))
                    return "🌐";
            }
            return "💻";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}