using Congressional_App_Test1.Resources.Classes;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;
using System.Windows.Shapes;
using System.Windows.Input;
using System;
using System.IO;
using System.Diagnostics;
using System.Reflection.Emit;
using Congressional_App_Test1.Presentation;
using Newtonsoft.Json;

using Path = System.IO.Path;
using File = System.IO.File;
using Congressional_App_Test1.Utilities;
using System.Windows.Documents;
using System.Windows.Media.Imaging;

public class AnimalInfo
{
    public string ImageUriSource { get; set; }
    public string Rarity { get; set; }
    public string HabitatDescription { get; set; }
}

internal class OrganismHabitatMap : Map
{

    string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
    private static string resultsFilePath;

    List<Point>? currentBounds;

    private List<(string speciesName, Rect boundingBox)> _habitats;
    private List<(string speciesName, double latitude, double longitude)> _speciesLocations;

    private TextBlock speciesLabel;
    private TextBlock rarityLabel;
    private Line infoLine;

    // Load animal information from the JSON file into a dictionary
    Dictionary<string, AnimalInfo> animalInfo;

    public OrganismHabitatMap(Canvas mapCanvas, Image mapImage, Border border, Canvas infoCanvas,
        List<(string speciesName, Rect boundingBox)> habitats, List<(string speciesName, double latitude, double longitude)> speciesLocations)
        : base(mapCanvas, mapImage, border)
    {
        _habitats = habitats;
        _speciesLocations = speciesLocations;
        _InfoCanvas = infoCanvas;

        resultsFilePath = Path.Combine(projectDirectory, "Resources", "Data", "baseAnimalData.json");

        speciesLabel = VisualTreeHelperExtensions.FindChild<TextBlock>(_InfoCanvas, "speciesLabel");
        rarityLabel = VisualTreeHelperExtensions.FindChild<TextBlock>(_InfoCanvas, "rarityLabel");
        infoLine = VisualTreeHelperExtensions.FindChild<Line>(_InfoCanvas, "infoLine");

        strokeSelect = new SolidColorBrush(Color.FromRgb(253, 255, 186));
        ogColor = new SolidColorBrush(Color.FromRgb(113, 217, 140));
        ogStroke = new SolidColorBrush(Color.FromRgb(186, 255, 204));

        // Load animal information from the JSON file into a dictionary
        if (File.Exists(resultsFilePath))
        {
            string json = File.ReadAllText(resultsFilePath);

            // Check if the file is not empty
            if (!string.IsNullOrWhiteSpace(json))
            {
                // Deserialize the JSON into a dictionary
                animalInfo = JsonConvert.DeserializeObject<Dictionary<string, AnimalInfo>>(json);
            }
        }

        if (_InfoCanvas != null)
        _InfoCanvas.Visibility = Visibility.Collapsed;
    }

    public override void CreateMarkers()
    {
        Matrix transformMatrix = _mapCanvas.RenderTransform.Value;

        // Create rectangles for habitats
        foreach (var habitat in _habitats)
        {
            var (speciesName, boundingBox) = habitat;

            var topLeftCornerLat = boundingBox.Right;
            var topLeftCornerLon = boundingBox.Top;
            var bottomRightCornerLat = boundingBox.Left;
            var bottomRightCornerLon = boundingBox.Bottom;

            // Convert top-left and bottom-right latitude and longitude to pixel coordinates
            var (topLeftX, topLeftY) = ConvertLatLonToPixels(topLeftCornerLat, topLeftCornerLon);
            var (bottomRightX, bottomRightY) = ConvertLatLonToPixels(bottomRightCornerLat, bottomRightCornerLon);

            // Calculate width and height based on pixel coordinates
            double width = Math.Abs(bottomRightX - topLeftX);
            double height = Math.Abs(bottomRightY - topLeftY);

            // Create the bounding box for the habitat
            Rectangle box = new Rectangle
            {
                Stroke = Brushes.DarkGreen,
                StrokeThickness = (1.15 * markerSizeScale) / _currentZoomLevel,
                Fill = new SolidColorBrush(Color.FromArgb(50, 0, 255, 0)),
                Visibility = Visibility.Collapsed // Initially hidden
            };

            // Set position and size of the bounding box
            Canvas.SetLeft(box, topLeftX);
            Canvas.SetTop(box, topLeftY);
            box.Width = width;
            box.Height = height;

            // Attach mouse events for interaction
            box.MouseEnter += (s, e) => Shape_MouseEnter(s, e);
            box.MouseLeave += (s, e) => Shape_MouseLeave(s, e);
            box.MouseDown += (s, e) => Shape_Clicked(s, e);

            // Attach the MouseMove event to make the InfoCanvas follow the mouse
            box.MouseMove += (s, e) => Shape_MouseMove(s, e);

            box.MouseLeave += (s, e) => HideInfoCanvas();
            box.MouseEnter += (s, e) => ShowInfoCanvas(s, e, speciesName);

            _mapCanvas.Children.Add(box);

            Panel.SetZIndex(box, 1); // Set ZIndex for marker to be higher
            ogSizes.Add(box, new List<(double, double)> { (box.Width, box.Height) });
        }

        // Create dots for species locations
        foreach (var speciesLocation in _speciesLocations)
        {
            var (speciesName, latitude, longitude) = speciesLocation;
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

            dot.MouseEnter += Shape_MouseEnter; // Show species info on hover and enlarge dot
            dot.MouseLeave += Shape_MouseLeave;
            dot.MouseDown += (s, e) => Shape_Clicked(s, e);

            dot.MouseEnter += (s, e) => ShowInfoCanvas(s, e, speciesName, transformedPoint);
            dot.MouseLeave += (s, e) => HideInfoCanvas();

            _mapCanvas.Children.Add(dot);
            Panel.SetZIndex(dot, 2); // Set ZIndex for marker to be higher
            ogSizes.Add(dot, new List<(double, double)> { (dot.Width, dot.Height) });

            // Add additional images (e.g., animalIcon, point, and glass) near the dot

            // Create the animal icon image using the image URI from the dictionary
            Image animalIcon = new Image();

            if (animalInfo.ContainsKey(speciesName))
            {
                // Get the ImageUriSource from the dictionary
                string imageUri = animalInfo[speciesName].ImageUriSource;
                //Debug.WriteLine(imageUri);

                // Set the ImageSource based on the species
                animalIcon.Source = new BitmapImage(new Uri(imageUri));
            }
            else
            {
                // Optionally set a default image if species is not found
                animalIcon.Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Animal Images/Red Fox.png"));
            }

            // Set the dimensions and clip
            animalIcon.Width = 26;
            animalIcon.Height = 25;
            animalIcon.Stretch = Stretch.Uniform;
            animalIcon.Clip = new EllipseGeometry { Center = new Point(12.5, 9), RadiusX = 8, RadiusY = 8 };

            // Position the icon
            Canvas.SetLeft(animalIcon, transformedPoint.X - 12.5); // Adjust position to overlap dot
            Canvas.SetTop(animalIcon, transformedPoint.Y - 33);
            
            animalIcon.Visibility = Visibility.Collapsed;

            // Add the animal icon to the canvas
            _mapCanvas.Children.Add(animalIcon);

            // Pointer Image
            Image pointerImage = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Map/pointer.png")),
                Width = 30,
                Height = 30,
                Stretch = Stretch.Uniform
            };
            Canvas.SetLeft(pointerImage, transformedPoint.X - 15);
            Canvas.SetTop(pointerImage, transformedPoint.Y - 30);

            pointerImage.Visibility = Visibility.Collapsed;
            //pointerImage.Effect

            _mapCanvas.Children.Add(pointerImage);

            // Glass Image
            Image glassImage = new Image
            {
                Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Map/glass.png")),
                Width = 30,
                Height = 30,
                Stretch = Stretch.Uniform
            };
            Canvas.SetLeft(glassImage, transformedPoint.X - 15);
            Canvas.SetTop(glassImage, transformedPoint.Y - 30);
            glassImage.Visibility = Visibility.Collapsed;
            _mapCanvas.Children.Add(glassImage);

            // Adjust ZIndex to ensure proper layering
            Panel.SetZIndex(animalIcon, 3); // Overlap on top
            Panel.SetZIndex(pointerImage, 2); // Below dot
            Panel.SetZIndex(glassImage, 2); // Below dot

            pointImages.TryAdd(dot, (pointerImage, glassImage, animalIcon));
        }
    }


    private void ShowInfoCanvas(object sender, MouseEventArgs e, string species, Point? position = null)
    {
        // Ensure InfoCanvas is on top
        //Panel.SetZIndex(_InfoCanvas, 3);

        // Set ZIndex: keep line under the text labels
        Panel.SetZIndex(infoLine, 0); // Lower ZIndex for the line
        Panel.SetZIndex(speciesLabel, 3); // Higher ZIndex for species label
        Panel.SetZIndex(rarityLabel, 3); // Higher ZIndex for rarity label

        // Use the provided position if it's passed, otherwise get the position from the mouse event
        Point _position = position ?? e.GetPosition(_mapCanvas);
        if (selectedMarker != null)
            return;


        Debug.Print(species);
        string rarity = "Common";
        //Dictionary<string, AnimalInfo> animalInfo;

        //if (File.Exists(resultsFilePath))
        //{
        //    string json = File.ReadAllText(resultsFilePath);

        //    // Check if the file is not empty
        //    if (!string.IsNullOrWhiteSpace(json))
        //    {
        //        // Deserialize the JSON into a dictionary
        //        animalInfo = JsonConvert.DeserializeObject<Dictionary<string, AnimalInfo>>(json);

        if (animalInfo != null)
        {
            Debug.Print(animalInfo.ToString());

            // Check if the species exists in the dictionary
            if (animalInfo.ContainsKey(species))
            {
                rarity = animalInfo[species].Rarity; // Get the rarity of the species
            }
        }
        //    }
        //}


        // Update the text of the quality and pH labels
        if (speciesLabel != null)
        {
            speciesLabel.Text = $"Species: {species}";
        }

        if (rarityLabel != null)
        {
            rarityLabel.Text = $"Frequency: {rarity}";
        }

        // Set the position of the InfoCanvas
        Canvas.SetLeft(_InfoCanvas, _position.X);
        Canvas.SetTop(_InfoCanvas, _position.Y);

        // Optionally, scale the InfoCanvas based on the zoom level
        _InfoCanvas.RenderTransform = new ScaleTransform(1 / _currentZoomLevel, 1 / _currentZoomLevel);

        // Make it visible
        _InfoCanvas.Visibility = Visibility.Visible;

        if (sender is Ellipse)
        {
            infoLine.Visibility = Visibility.Visible;
        }
        else if (sender is Rectangle)
        {
            infoLine.Visibility = Visibility.Collapsed;
        }
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
            else if (child is Rectangle r)
            {
                //Debug.WriteLine(r.ToString());
                r.StrokeThickness = (1.15 * markerSizeScale) / _currentZoomLevel;
            }
        }
    }

    // Handle mouse enter to enlarge dots or highlight bounding boxes
    private void Shape_MouseEnter(object sender, MouseEventArgs e)
    {
        if (selectedMarker != null)
            return;

        if (sender is Ellipse dot)
        {
            dot.Fill = Brushes.Red;
            dot.StrokeThickness = 1.5 * markerSizeScale;

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
                scaleTransform.ScaleX,
                (1 / _currentZoomLevel) * 1.5,
                50,
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleX = value;
                    scaleTransform.ScaleY = value;
                }
            );

            tweenToEnlarge.OnStop += () => _shapeTweens.Remove(dot);
            _shapeTweens[dot] = tweenToEnlarge;
            tweenToEnlarge.Start();
        }
        else if (sender is Rectangle r)
        {
            r.Stroke = Brushes.LightGreen;
        }
    }

    // Handle mouse leave to shrink dots or reset bounding box colors
    private void Shape_MouseLeave(object sender, MouseEventArgs e)
    {
        if (selectedMarker != null)
            return;

        if (sender is Ellipse dot)
        {
            if (_shapeTweens.TryGetValue(dot, out Tween? existingTween))
            {
                existingTween.Stop();
                _shapeTweens.Remove(dot);
            }

            ScaleTransform scaleTransform = (ScaleTransform)dot.RenderTransform;

            Tween tweenToShrink = new Tween(
                scaleTransform.ScaleX,
                1 / _currentZoomLevel,
                50,
                EasingStyle.Quadratic,
                value =>
                {
                    scaleTransform.ScaleX = value;
                    scaleTransform.ScaleY = value;
                }
            );

            tweenToShrink.OnStop += () => _shapeTweens.Remove(dot);
            dot.Fill = ogColor;
            dot.StrokeThickness = 1.15 * markerSizeScale;
            _shapeTweens[dot] = tweenToShrink;
            tweenToShrink.Start();
        }
        else if (sender is Rectangle r)
        {
            r.Stroke = Brushes.DarkGreen;
            r.StrokeThickness = (1.15 * markerSizeScale) / _currentZoomLevel;
        }
    }

    private void Shape_Clicked(object sender, RoutedEventArgs e)
    {
        if (sender is Shape s)
        {
            if (selectedMarker == null)
            {
                selectedMarker = s;
                s.Stroke = strokeSelect;
                if (s is Rectangle)
                    s.StrokeThickness = (1.75 * markerSizeScale) / _currentZoomLevel;
                else
                    s.StrokeThickness = 1.75 * markerSizeScale;
            }
            else
            {
                s.Stroke = ogStroke;
                if (s is Rectangle)
                {
                    s.Stroke = Brushes.DarkGreen;
                }
                selectedMarker = null;
                HideInfoCanvas();
                Shape_MouseLeave(sender, new MouseEventArgs(Mouse.PrimaryDevice, 0) { RoutedEvent = Mouse.MouseLeaveEvent });
            }
        }
    }

    private void Shape_MouseMove(object sender, MouseEventArgs e)
    {
        if (selectedMarker != null)
            return;

        if (sender is Rectangle)
        {
            Point mousePosition = e.GetPosition(_mapCanvas);

            // Set the position of the InfoCanvas to follow the mouse
            Canvas.SetLeft(_InfoCanvas, mousePosition.X);
            Canvas.SetTop(_InfoCanvas, mousePosition.Y);

            // Optionally, scale the InfoCanvas based on the zoom level
            _InfoCanvas.RenderTransform = new ScaleTransform(1 / _currentZoomLevel, 1 / _currentZoomLevel);
        }
    }

    protected override void Dot_Clicked(object sender, RoutedEventArgs e, bool oride = false)
    {
        throw new NotImplementedException();
    }
}
