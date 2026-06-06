using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using System;
using System.Collections.Generic;

namespace WeShare.UI.Controls
{
    public class SpeedGraphControl : Control
    {
        public static readonly StyledProperty<IEnumerable<double>> SpeedPointsProperty =
            AvaloniaProperty.Register<SpeedGraphControl, IEnumerable<double>>(
                nameof(SpeedPoints),
                defaultValue: Array.Empty<double>());

        static SpeedGraphControl()
        {
            AffectsRender<SpeedGraphControl>(SpeedPointsProperty);
        }

        public IEnumerable<double> SpeedPoints
        {
            get => GetValue(SpeedPointsProperty);
            set => SetValue(SpeedPointsProperty, value);
        }

        protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
        {
            base.OnPropertyChanged(change);

            if (change.Property == SpeedPointsProperty)
            {
                if (change.OldValue is System.Collections.Specialized.INotifyCollectionChanged oldColl)
                {
                    oldColl.CollectionChanged -= SpeedPoints_CollectionChanged;
                }
                if (change.NewValue is System.Collections.Specialized.INotifyCollectionChanged newColl)
                {
                    newColl.CollectionChanged += SpeedPoints_CollectionChanged;
                }
                InvalidateVisual();
            }
        }

        private void SpeedPoints_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            InvalidateVisual();
        }

        public void AddSpeed(double speed)
        {
            if (SpeedPoints is System.Collections.ObjectModel.ObservableCollection<double> coll)
            {
                coll.Add(speed);
                if (coll.Count > 40)
                {
                    coll.RemoveAt(0);
                }
            }
            else
            {
                var newColl = new System.Collections.ObjectModel.ObservableCollection<double> { speed };
                SpeedPoints = newColl;
            }
        }

        public void Clear()
        {
            if (SpeedPoints is System.Collections.ObjectModel.ObservableCollection<double> coll)
            {
                coll.Clear();
            }
            else
            {
                SpeedPoints = new System.Collections.ObjectModel.ObservableCollection<double>();
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            var bounds = Bounds;
            double w = bounds.Width;
            double h = bounds.Height;

            if (w <= 0 || h <= 0) return;

            // Background
            context.FillRectangle(new SolidColorBrush(Color.FromArgb(12, 0, 0, 0)), new Rect(0, 0, w, h), 4);

            // Border
            context.DrawRectangle(null, new Pen(new SolidColorBrush(Color.Parse("#40FFFFFF")), 1), new Rect(0, 0, w, h), 4);

            var pointsList = new List<double>();
            if (SpeedPoints != null)
            {
                foreach (var s in SpeedPoints)
                {
                    pointsList.Add(s);
                }
            }

            if (pointsList.Count == 0) return;

            double maxSpeed = 5;
            foreach (var s in pointsList)
            {
                if (s > maxSpeed) maxSpeed = s;
            }

            // Grid lines
            var gridPen = new Pen(new SolidColorBrush(Color.FromArgb(20, 255, 255, 255)), 1);
            for (int i = 1; i < 4; i++)
            {
                double gy = (h / 4) * i;
                context.DrawLine(gridPen, new Point(0, gy), new Point(w, gy));
            }

            // Draw line
            double step = w / 39.0;
            var points = new List<Point>();
            int startIndex = 40 - pointsList.Count;

            for (int i = 0; i < pointsList.Count; i++)
            {
                double x = (startIndex + i) * step;
                double y = h - (pointsList[i] / maxSpeed) * (h - 20) - 10;
                points.Add(new Point(x, y));
            }

            var linePen = new Pen(new SolidColorBrush(Color.Parse("#107C41")), 2); // Windows Green
            
            for (int i = 0; i < points.Count - 1; i++)
            {
                context.DrawLine(linePen, points[i], points[i + 1]);
            }

            // Fill area
            var fillBrush = new SolidColorBrush(Color.FromArgb(30, 16, 124, 65));
            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(new Point(points[0].X, h), true);
                for (int i = 0; i < points.Count; i++)
                {
                    ctx.LineTo(points[i]);
                }
                ctx.LineTo(new Point(points[^1].X, h));
                ctx.EndFigure(true);
            }
            context.DrawGeometry(fillBrush, null, geometry);
        }
    }
}
