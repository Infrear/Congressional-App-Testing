using Congressional_App_Test1.Resources.Classes;
using Congressional_App_Test1.Utilities;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows;
using Congressional_App_Test1;
using System.Diagnostics;
using static System.Net.WebRequestMethods;
using System;

internal class WaterQualityMap : Map
{

    private List<(string name, double latitude, double longitude, double dissolvedOxy, double pH, double temperature, string dateTime)> _locations;
    private TextBlock? qualityLabel;
    private TextBlock? pHLabel;
    private Button? moreInfoBtn;
    Random r = new Random();

        public WaterQualityMap(Canvas mapCanvas, Image mapImage, Border border, Canvas infoCanvas,
        List<(string name, double latitude, double longitude, double dissolvedOxy, double pH, double temperature, string dateTime)> locations)
        : base(mapCanvas, mapImage, border)
    {
        _locations = locations;
        _InfoCanvas = infoCanvas;

        // Find the required UI elements
        qualityLabel = VisualTreeHelperExtensions.FindChild<TextBlock>(_InfoCanvas, "qualityLabel");
        pHLabel = VisualTreeHelperExtensions.FindChild<TextBlock>(_InfoCanvas, "pHLabel");
        moreInfoBtn = VisualTreeHelperExtensions.FindChild<Button>(_InfoCanvas, "moreInfoBtn");
    }

    public override void CreateMarkers()
    {
        Matrix transformMatrix = _mapCanvas.RenderTransform.Value;

        foreach (var location in _locations)
        {
            var (name, latitude, longitude, dissolvedOxy, pH, temperature, dateTime) = location;
            if (!dots.ContainsKey(name))
            {
                var (x, y) = ConvertLatLonToPixels(latitude, longitude);

            Point transformedPoint = transformMatrix.Transform(new Point(x, y));

            Ellipse dot = new Ellipse
            {
                Fill = ogColor,
                Stroke = ogStroke,
                Width = 10 * markerSizeScale,
                Height = 10 * markerSizeScale,
                StrokeThickness = 1.15 * markerSizeScale,
                Visibility = Visibility.Collapsed // Initially hidden
            };

            dot.RenderTransformOrigin = new Point(0.5, 0.5);
            dot.RenderTransform = new ScaleTransform(1, 1);

            Canvas.SetLeft(dot, transformedPoint.X - (dot.Width / 2));
            Canvas.SetTop(dot, transformedPoint.Y - (dot.Height / 2));

            dot.MouseEnter += (s, e) => Dot_MouseEnter(s, e);
            dot.MouseLeave += (s, e) => Dot_MouseLeave(s, e);
            dot.MouseDown += (s, e) => Dot_Clicked(s, e);

            dot.MouseEnter += (s, e) => ShowInfoCanvas(transformedPoint, dissolvedOxy, pH);
            dot.MouseLeave += (s, e) => HideInfoCanvas();


            moreInfoBtn.Click += (sender, e) =>
            {
                if (selectedMarker == dot)
                {
                    MapPage.Invoke_Load(name, pH, dissolvedOxy, temperature, dateTime);
                }
            };

            _mapCanvas.Children.Add(dot);
            ogSizes.Add(dot, new List<(double, double)> { (dot.Width, dot.Height) });

            double a = Canvas.GetLeft(dot);
            double b = Canvas.GetTop(dot);
            Debug.WriteLine(name);
            // Create a Point object to represent the dot's position on the canvas
            Point dotPosition = new Point(a, b);
            dots.Add(name, (dot, dotPosition, transformedPoint, dissolvedOxy, pH));

                if (_InfoCanvas != null)
                    _InfoCanvas.Visibility = Visibility.Collapsed;
            }
        }
    }

    public void ShowInfoCanvas(Point position, double dOx, double pH)
    {
        if (selectedMarker != null)
            return;
        
        // Update the text of the quality and pH labels
        if (qualityLabel != null)
        {
            string rDissolvedOxygen;
            if (dOx == 0)
                rDissolvedOxygen = "--"; //Math.Round(10 + r.NextDouble() * 3, 1); // Range: 10.0 to 13.0
            else
                rDissolvedOxygen = dOx.ToString("0.00");

            qualityLabel.Text = $"Dissolved Oxygen: {rDissolvedOxygen}";
        }

        if (pHLabel != null)
        {
            string rPH;
            if (pH == 0)
                rPH = "--";//Math.Round(6.5 + r.NextDouble() * 2, 1); // Range: 6.5 to 8.5
            else
                rPH = pH.ToString("0.00");

            pHLabel.Text = $"pH: {rPH}"; // Display pH value to 2 decimal places
        }

        // Set the position of the InfoCanvas
        Canvas.SetLeft(_InfoCanvas, position.X);
        Canvas.SetTop(_InfoCanvas, position.Y);

        // Optionally, scale the InfoCanvas based on the zoom level
        _InfoCanvas.RenderTransform = new ScaleTransform(1 / _currentZoomLevel, 1 / _currentZoomLevel);

        // Make it visible
        _InfoCanvas.Visibility = Visibility.Visible;
    }

    protected override void ScaleMarkers()
    {
        foreach (UIElement child in _mapCanvas.Children)
        {
            if (child is Ellipse dot)
            {
                ScaleTransform? scaleTransform = dot.RenderTransform as ScaleTransform ?? new ScaleTransform(1, 1);
                dot.RenderTransform = scaleTransform;
                dot.RenderTransformOrigin = new Point(0.5, 0.5);

                double scaleFactor = 1 / _currentZoomLevel;
                scaleTransform.ScaleX = scaleFactor;
                scaleTransform.ScaleY = scaleFactor;
                dot.StrokeThickness = 1.15 * markerSizeScale;
            }
        }
    }

    // Handle mouse enter to enlarge dots
    private void Dot_MouseEnter(object sender, MouseEventArgs e)
    {
        if (selectedMarker != null)
            return;

        if (sender is Ellipse dot)
        {
            dot.Fill = Brushes.Red; // Change to red on hover
            dot.StrokeThickness = 1.5 * markerSizeScale; // Optionally, increase stroke thickness on hover

            // Stop any existing tween for this dot
            if (_shapeTweens.TryGetValue(dot, out Tween? existingTween))
            {
                existingTween.Stop();
                _shapeTweens.Remove(dot);
            }

            // Get the scale transform
            ScaleTransform scaleTransform = (ScaleTransform)dot.RenderTransform;

            // Create a new tween to enlarge the dot
            Tween tweenToEnlarge = new Tween(
                scaleTransform.ScaleX, // Start from current scale
                (1 / _currentZoomLevel) * 1.5, // Desired scale
                50, // Duration in milliseconds
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleX = value; // Update the scale
                    scaleTransform.ScaleY = value; // Update the scale
                }
            );

            tweenToEnlarge.OnStop += () =>
            {
                // Remove the tween from the dictionary
                _shapeTweens.Remove(dot);
            };

            _shapeTweens[dot] = tweenToEnlarge; // Store the tween
            tweenToEnlarge.Start(); // Start the enlarge tween
        }
    }


    // Handle mouse leave to shrink dots back to their original size
    private void Dot_MouseLeave(object sender, MouseEventArgs e)
    {
        if (selectedMarker != null)
            return;

        if (sender is Ellipse dot)
        {
            // Stop any existing tween for this dot
            if (_shapeTweens.TryGetValue(dot, out Tween? existingTween))
            {
                existingTween.Stop();
                _shapeTweens.Remove(dot);
            }

            // Get the scale transform
            ScaleTransform scaleTransform = (ScaleTransform)dot.RenderTransform;

            // Create a new tween to shrink the dot back to its original size
            Tween tweenToShrink = new Tween(
                scaleTransform.ScaleX, // Start from current scale
                1 / _currentZoomLevel, // Desired scale (back to original size)
                50, // Duration in milliseconds
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleX = value; // Update the scale
                    scaleTransform.ScaleY = value; // Update the scale
                }
            );

            tweenToShrink.OnStop += () =>
            {
                // Remove the tween from the dictionary
                _shapeTweens.Remove(dot);
            };

            dot.Fill = ogColor; // Change to original color
            dot.StrokeThickness = 1.15 * markerSizeScale; // Optionally, decrease stroke thickness on un-hover

            _shapeTweens[dot] = tweenToShrink; // Store the tween
            tweenToShrink.Start(); // Start the shrink tween
        }
    }

    protected override void Dot_Clicked(object sender, RoutedEventArgs e, bool oride = false)
    {
        if (sender is Ellipse dot)
        {
            if (selectedMarker == null || oride)
            {
                selectedMarker = dot;
                dot.Stroke = strokeSelect;
                dot.StrokeThickness = 1.75 * markerSizeScale;
            }
            else
            {
                dot.Stroke = ogStroke;
                selectedMarker = null;
                HideInfoCanvas();
                Dot_MouseLeave(sender, new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent });
            }
        }
    }
}
