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
            if (values.Count < 3 || values[0] is not WeShare.Core.Models.DeviceModel device || values[1] is not System.Collections.IList devices || values[2] is not string type) 
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
            if (value is string type && !string.IsNullOrEmpty(type))
            {
                string lower = type.ToLowerInvariant();
                return lower switch
                {
                    "phone" or "android" or "ios" or "mobile" => "📱",
                    "mac" or "apple" or "macos" => "💻",
                    "linux" => "🐧",
                    "web client" or "web" or "browser" => "🌐",
                    "tablet" or "ipad" => "📱",
                    _ when lower.Contains("windows") || lower.Contains("pc") || lower.Contains("desktop") => "💻",
                    _ => "💻"
                };
            }
            return "💻";
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class TransferDirectionLabelConverter : IMultiValueConverter
    {
        public object? Convert(System.Collections.Generic.IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
        {
            if (values.Count >= 2 && values[0] is string peerName && values[1] is WeShare.Core.Models.TransferDirection direction)
            {
                string prefix = direction == WeShare.Core.Models.TransferDirection.Sent ? "To" : "From";
                return $"{prefix}: {peerName}";
            }
            if (values.Count >= 1 && values[0] is string name)
                return $"From: {name}";
            return "Unknown";
        }

        public object?[] ConvertBack(object? value, Type[] targetTypes, object? parameter, CultureInfo culture) => throw new NotImplementedException();
    }

}