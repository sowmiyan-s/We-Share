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

    public class DeviceTypeToGeometryConverter : IValueConverter
    {
        private static readonly Avalonia.Media.StreamGeometry DeviceGeom = Avalonia.Media.StreamGeometry.Parse("M4,6 H20 A2,2 0 0,1 22,8 V16 A2,2 0 0,1 20,18 H4 A2,2 0 0,1 2,16 V8 A2,2 0 0,1 4,6 Z M2,18 H22");
        private static readonly Avalonia.Media.StreamGeometry MobileGeom = Avalonia.Media.StreamGeometry.Parse("M12,18 H12.01 M8,21 H16 A2,2 0 0,0 18,19 V5 A2,2 0 0,0 16,3 H8 A2,2 0 0,0 6,5 V19 A2,2 0 0,0 8,21 Z");
        private static readonly Avalonia.Media.StreamGeometry GlobeGeom = Avalonia.Media.StreamGeometry.Parse("M12,2 A10,10 0 1,0 22,12 A10,10 0 0,0 12,2 Z M2,12 H22 M12,2 A15,15 0 0,1 16,12 A15,15 0 0,1 12,22 A15,15 0 0,1 8,12 A15,15 0 0,1 12,2 Z");

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is string type && !string.IsNullOrEmpty(type))
            {
                string lower = type.ToLowerInvariant();
                return lower switch
                {
                    "phone" or "android" or "ios" or "mobile" or "tablet" or "ipad" => MobileGeom,
                    "web client" or "web" or "browser" => GlobeGeom,
                    _ => DeviceGeom
                };
            }
            return DeviceGeom;
        }

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotImplementedException();
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
                    "phone" or "android" or "ios" or "mobile" => "Mobile",
                    "mac" or "apple" or "macos" => "Mac",
                    "linux" => "Linux",
                    "web client" or "web" or "browser" => "Web",
                    "tablet" or "ipad" => "Tablet",
                    _ => "PC"
                };
            }
            return "PC";
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