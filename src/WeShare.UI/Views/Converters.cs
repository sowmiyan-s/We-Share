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

            // Radar radii that comfortably fit within 480x360 canvas
            double[] rings = { 70, 115, 145 }; 
            double radius = rings[index % rings.Length];
            
            // Distribute peers symmetrically around the radar
            int total = Math.Max(devices.Count, 1);
            double baseStep = 360.0 / total;
            double angleDeg = index * baseStep - 90; // Start at top
            double angle = angleDeg * (Math.PI / 180.0);
            
            // Canvas Center (480x360 radar panel -> 240x180 center)
            double centerX = 240;
            double centerY = 180;

            // Marker width is ~80px (44px circle + label), height is ~64px
            if (type == "X") return centerX + Math.Cos(angle) * radius - 40;
            return centerY + Math.Sin(angle) * radius - 26;
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
                    return "MOB";
                if (lower.Contains("android"))
                    return "MOB";
                if (lower.Contains("mac") || lower.Contains("os x") || lower.Contains("osx"))
                    return "MAC";
                if (lower.Contains("linux"))
                    return "LNX";
                if (lower.Contains("windows") || lower.Contains("win"))
                    return "PC";
                if (lower.Contains("web"))
                    return "WEB";
            }
            return "DEV";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}