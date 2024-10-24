using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Path = System.IO.Path;
using File = System.IO.File;
using Newtonsoft.Json;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using Congressional_App_Test1.Presentation;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Xml.Linq;

namespace Congressional_App_Test1.Resources.Classes
{

    internal abstract class Map
    {
        protected Canvas _mapCanvas;
        protected Image _mapImage;
        protected Border _border;
        protected Canvas _InfoCanvas;
        protected double _currentZoomLevel = 1.0;
        protected const double MaxZoom = 35.0;
        protected const double MinZoom = 0.5;
        protected Point _origin, _start;
        protected double markerSizeScale = 2.0;
        protected Shape? selectedMarker;
        protected SolidColorBrush ogColor = new SolidColorBrush(Color.FromRgb(93, 107, 241));
        protected SolidColorBrush ogStroke = new SolidColorBrush(Color.FromRgb(44, 54, 106));
        protected SolidColorBrush strokeSelect = new SolidColorBrush(Color.FromRgb(255, 138, 138));
        protected SolidColorBrush grayIsh = new SolidColorBrush(Color.FromRgb(205, 205, 205));
        protected Dictionary<Shape, Tween> _shapeTweens = new Dictionary<Shape, Tween>();
        protected Dictionary<Canvas, Tween> _canvasTweens = new Dictionary<Canvas, Tween>(); 
        protected Dictionary<Shape, List<(double x, double y)>> ogSizes = new Dictionary<Shape, List<(double, double)>>();
        public Dictionary<string, (Ellipse, Point, Point, double, double)> dots = new Dictionary<string, (Ellipse, Point, Point, double, double)>();
        protected Dictionary<Shape, (Image pointer, Image glass, Image icon)> pointImages = new Dictionary<Shape, (Image, Image, Image)>();
       
        Dictionary<string, dynamic>? userInfo;
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;

        private float distThreshold = 250F;

        public Map(Canvas mapCanvas, Image mapImage, Border border)
        {

            resultsFilePath = Path.Combine(projectDirectory, "Resources", "Data", "baseUserData.json");

            userInfo = NamePage.LoadOldUserInfoData();
            _mapCanvas = mapCanvas;
            _mapImage = mapImage;
            _border = border;

            // Attach event handlers for panning and zooming
            _mapCanvas.MouseWheel += MainWindow_MouseWheel;
            _mapImage.MouseLeftButtonDown += image_MouseLeftButtonDown;
            _mapImage.MouseLeftButtonUp += image_MouseLeftButtonUp;
            _mapImage.MouseMove += image_MouseMove;

            // Add mouse move event handler for visibility control
            _mapCanvas.MouseMove += MapCanvas_MouseMove;

            // Zoom to the center of the map with a zoom level of 10 over 1 second
            //ZoomToPoint(new Point(_mapCanvas.ActualWidth / 2, _mapCanvas.ActualHeight / 2), 10, 1000);
        }

        private void MapCanvas_MouseMove(object sender, MouseEventArgs e)
        {
            Point mousePosition = e.GetPosition(_mapCanvas);

            foreach (UIElement child in _mapCanvas.Children)
            {
                if (child is Shape marker)
                {
                    // Calculate the distance from the mouse pointer to the marker
                    Point markerPosition = new Point(Canvas.GetLeft(marker), Canvas.GetTop(marker));
                    double distance = Math.Sqrt(Math.Pow(mousePosition.X - markerPosition.X, 2) + Math.Pow(mousePosition.Y - markerPosition.Y, 2));

                    // Set visibility based on distance (e.g., 50 pixels)
                    if (distance <= distThreshold)
                    {
                        if (marker.Visibility == Visibility.Collapsed) // If marker is currently hidden
                        {
                            marker.Visibility = Visibility.Visible; // Show marker
                            marker.RenderTransform = new ScaleTransform(0, 0);
                            BounceMarker(marker); // Trigger bounce animation
                        }
                    }
                    else if ((_InfoCanvas.Visibility != Visibility.Visible) && (!(_shapeTweens.TryGetValue(marker, out Tween? existingTween))))
                    {
                        if (marker.Visibility == Visibility.Visible) // If marker is currently visible
                        {
                            ShrinkMarker(marker); // Trigger shrink animation
                        }
                    }
                }
            }
        }


        // Abstract method for creating map-specific markers (e.g., dots, bounding boxes)
        public abstract void CreateMarkers();

        protected void image_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (_mapImage.IsMouseCaptured) return;

            _mapImage.CaptureMouse();
            _start = e.GetPosition(_border);
            _origin.X = _mapCanvas.RenderTransform.Value.OffsetX;
            _origin.Y = _mapCanvas.RenderTransform.Value.OffsetY;
        }

        protected void image_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_mapImage.IsMouseCaptured) return;

            Point p = e.GetPosition(_border);
            Matrix m = _mapCanvas.RenderTransform.Value;
            m.OffsetX = _origin.X + (p.X - _start.X);
            m.OffsetY = _origin.Y + (p.Y - _start.Y);
            _mapCanvas.RenderTransform = new MatrixTransform(m);
        }

        protected void image_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_mapImage.IsMouseCaptured) return;
            _mapImage.ReleaseMouseCapture();
        }

        protected void MainWindow_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point p = e.GetPosition(_mapCanvas);
            Matrix m = _mapCanvas.RenderTransform.Value;
            double zoomFactor = e.Delta > 0 ? 1.1 : 1 / 1.1;

            double newScaleX = m.M11 * zoomFactor;
            double newScaleY = m.M22 * zoomFactor;
            newScaleX = Math.Max(MinZoom, Math.Min(MaxZoom, newScaleX));
            newScaleY = Math.Max(MinZoom, Math.Min(MaxZoom, newScaleY));

            m.ScaleAtPrepend(newScaleX / m.M11, newScaleY / m.M22, p.X, p.Y);
            _mapCanvas.RenderTransform = new MatrixTransform(m);

            _currentZoomLevel = newScaleX;

            // Call method to scale markers (override in subclasses)
            ScaleMarkers();
        }

        // Abstract method for scaling markers (dots, boxes) based on zoom level
        protected abstract void ScaleMarkers();

        protected abstract void Dot_Clicked(object sender, RoutedEventArgs e, bool oride = false);

        protected void HideInfoCanvas()
        {
            if (selectedMarker != null)
                return;

            _InfoCanvas.Visibility = Visibility.Collapsed;
        }

        // Utility method for converting latitude/longitude to pixel coordinates
        public (double x, double y) ConvertLatLonToPixels(double latitude, double longitude, double imageWidth = 10500, double imageHeight = 6038)
        {
            double x = (imageWidth * (180 + longitude)) / 360;
            double y = (imageHeight * (90 - latitude)) / 180;
            return (x, y);
        }


        private void BounceMarker(Shape marker)
        {
            if (!ogSizes.TryGetValue(marker, out var originalSizeList) || originalSizeList.Count == 0)
                return;

            // Stop any existing tween for this dot
            if (_shapeTweens.TryGetValue(marker, out Tween? existingTween))
            {
                existingTween.Stop();
                _shapeTweens.Remove(marker);
            }

            var (originalWidth, originalHeight) = (1, 1);
            // Set scale transform to the original size
            ScaleTransform scaleTransform = (ScaleTransform)marker.RenderTransform;

            // Create a new tween to enlarge the dot
            Tween bounceTween = new Tween(
                scaleTransform.ScaleY,
                ((1 / _currentZoomLevel))*1.5, // Bounce height (150% of original size)
                200, // Duration
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleY = value;
                    scaleTransform.ScaleX = value; // To maintain aspect ratio
                },
                () =>
                {
                    if (pointImages.TryGetValue(marker, out var imgs))
                    {
                        {
                            var (img1, img2, img3) = imgs;
                            img1.Visibility = Visibility.Visible;
                            img2.Visibility = Visibility.Visible;
                            img3.Visibility = Visibility.Visible;
                        }
                    }

                    // After the bounce up, shrink it back to the original size
                    Tween returnTween = new Tween(
                        scaleTransform.ScaleY,
                        (1 / _currentZoomLevel), // Return to original size
                        200, // Duration
                        EasingStyle.Quadratic,
                        value =>
                        {
                            scaleTransform.ScaleY = value;
                            scaleTransform.ScaleX = value; // To maintain aspect ratio

                            _shapeTweens.Remove(marker);
                        }
                    );

                    returnTween.Start();
                }
            );

            _shapeTweens[marker] = bounceTween; // Store the tween
            bounceTween.Start();
        }

        private void ShrinkMarker(Shape marker)
        {
            if (!ogSizes.TryGetValue(marker, out var originalSizeList) || originalSizeList.Count == 0)
                return;

            // Stop any existing tween for this dot
            if (_shapeTweens.TryGetValue(marker, out Tween? existingTween))
            {
                existingTween.Stop();
                _shapeTweens.Remove(marker);
            }

            var (originalWidth, originalHeight) = originalSizeList[0];

            ScaleTransform scaleTransform = (ScaleTransform)marker.RenderTransform;

            // Create a new tween to shrink the dot
            Tween shrinkTween = new Tween(
                scaleTransform.ScaleX,
                0, // Shrink to nothing
                200, // Duration
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleX = value;
                    scaleTransform.ScaleY = value; // To maintain aspect ratio

                    // Hide the marker once it reaches a size of zero
                    if (value <= 0)
                    {
                        marker.Visibility = Visibility.Collapsed;

                        if (pointImages.TryGetValue(marker, out var imgs))
                        {
                            {
                                var (img1, img2, img3) = imgs;
                                img1.Visibility = Visibility.Collapsed;
                                img2.Visibility = Visibility.Collapsed;
                                img3.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }
            );

            _shapeTweens[marker] = shrinkTween; // Store the tween
            shrinkTween.Start();
        }

        public void ZoomToPoint(Point targetPoint, double targetZoomLevel, int duration = 500, bool select = true, Ellipse? dotToSelect = null, double dissolvedOxygen = 0, double pH = 0, Point? tpoint = null, string rLoc = "", string stationNumber = "12345")
        {
            Ellipse? dot = dotToSelect;
            Point transformedPoint;
            if (tpoint != null)
                transformedPoint = (Point)tpoint;
            if (targetZoomLevel < MinZoom || targetZoomLevel > MaxZoom)
                return; // Ensure the zoom level is within bounds

            // Stop any existing tweens for the map canvas zoom

            // Start tween for smooth zoom
            double initialZoomLevel = _currentZoomLevel;
            double zoomFactor = targetZoomLevel / initialZoomLevel;

            Point currentPosition = new Point(_mapCanvas.RenderTransform.Value.OffsetX, _mapCanvas.RenderTransform.Value.OffsetY);

            // Get the scale transform
            ScaleTransform scaleTransform = new ScaleTransform();
            double start = 0;

            if (select && dot != null)
            {
                dot.Fill = Brushes.LightGoldenrodYellow; // Change to red on hover
                dot.StrokeThickness = 1.7 * markerSizeScale; // Optionally, increase stroke thickness on hover

                //((WaterQualityMap)this).ShowInfoCanvas(targetPoint, dissolvedOxygen, pH);
            }

            if (select && dot != null)
             {  
                scaleTransform = (ScaleTransform)dot.RenderTransform;
                start = scaleTransform.ScaleX;
                // Stop any existing tween for this dot
                if (_shapeTweens.TryGetValue(dot, out Tween? existingTween))
                {
                    existingTween.Stop();
                    _shapeTweens.Remove(dot);
                    dot.Visibility = Visibility.Visible;
                }
            }

            Tween zoomTween = new Tween(
                0, 1, duration, // ms
                EasingStyle.Quadratic,
                value =>
                {
                    double zoomLerp = Single.Lerp((float)initialZoomLevel, (float)targetZoomLevel, (float)value); // Interpolate zoom level
                    double scaleFactor = zoomLerp / _currentZoomLevel; // Get how much to scale based on interpolation

                    // get the matrix for current render transform
                    Matrix m = _mapCanvas.RenderTransform.Value;

                    // zoom with respect to the target point 
                    m.ScaleAtPrepend(scaleFactor, scaleFactor, targetPoint.X, targetPoint.Y);

                    _mapCanvas.RenderTransform = new MatrixTransform(m);
                    _currentZoomLevel = zoomLerp; 

                    if (select && dot != null)
                    {
                        scaleTransform.ScaleX = Math.Clamp(((1-value) / _currentZoomLevel) * 1.5, (1 / targetZoomLevel) * 1.5, Double.MaxValue); // Update the scale
                        scaleTransform.ScaleY = Math.Clamp(((1-value) / _currentZoomLevel) * 1.5, (1 / targetZoomLevel) * 1.5, Double.MaxValue); // Update the scale
                    }
                },
                () =>
                {

                    ScaleMarkers();

                    if ((WaterQualityMap)this != null)
                    {
                        WaterQualityMap me = (WaterQualityMap)this;
                        me.ShowInfoCanvas(transformedPoint, dissolvedOxygen, pH);
                    }
                    selectedMarker = dot;
                    Dot_Clicked(dot, new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseDownEvent }, true);
                    // Optional: Action when the zoom completes, maybe reset tweens or do additional handling
                }
            );
            if (File.Exists(resultsFilePath))
            {
                Dictionary<string, dynamic>? existingData = NamePage.LoadOldUserInfoData();

                string preJson = File.ReadAllText(resultsFilePath);
                Debug.WriteLine(preJson);

                Debug.WriteLine("1");
                if (existingData.ContainsKey("Visits"))
                {
                    Debug.WriteLine("2");

                    // Deserialize Visits as JObject first
                    var visitsJObject = existingData["Visits"] as JObject;
                    if (visitsJObject != null)
                    {
                        Debug.WriteLine("3");

                        // Convert JObject to Dictionary<string, Dictionary<string, string>>
                        var visitsData = visitsJObject.ToObject<Dictionary<string, Dictionary<string, string>>>();

                        if (visitsData != null)
                        {
                            Debug.WriteLine("4");
                            string str = Regex.Replace(Regex.Unescape(rLoc), @"\t|\n|\r", "");
                            if (!visitsData.ContainsKey(str))
                            {
                                // Add new visit details using rLoc
                                visitsData.Add(
                                    str,  // Key for the new visit
                                    new Dictionary<string, string>
                                    {
                                    { "Date", DateTime.Now.ToString("dd/MM/yyyy") },
                                    { "Time", DateTime.Now.ToString("HH:mm:ss") },
                                    { "pH", pH.ToString() },
                                    { "DissolvedOxygen", dissolvedOxygen.ToString() },
                                    { "StationNumber", stationNumber.ToString() },
                                    }
                                );
                            }

                            existingData["Visits"] = visitsData;
                        }
                        else
                        {
                            Debug.WriteLine("visitsData is null after conversion");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("visitsJObject is null");
                    }
                }
                else
                {
                    Debug.WriteLine("existingData does not contain 'Visits'");
                }

                //Debug.WriteLine(existingData["Visits"].GetType().ToString());

                // Serialize and write the updated data back to the file
                string json = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                File.WriteAllText(resultsFilePath, json);
            }
            zoomTween.Start();
        }
    }
}
