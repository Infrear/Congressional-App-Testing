using Congressional_App_Test1.Resources.Classes;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Congressional_App_Test1.Presentation
{
    /// <summary>
    /// Interaction logic for About.xaml
    /// </summary>
    /// 
    public partial class About : Page
    {
        // Load animal information from the JSON file into a dictionary
        Dictionary<string, dynamic>? userInfo;

        // Get a reference to the main window
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;

        public About()
        {
            Loaded += (s, e) =>
            {
                InitializeComponent();

                Dictionary<string, dynamic>? retrievedUserInfo = NamePage.LoadOldUserInfoData();

                var visitsJObject = retrievedUserInfo["Visits"] as JObject;
                if (visitsJObject != null)
                {
                    // water data
                    var visitsData = visitsJObject.ToObject<Dictionary<string, Dictionary<string, string>>>();

                    if (visitsData != null)
                    {
                        DisplayWaterData(visitsData);
                    }

                    // animal data
                    resultsFilePath = System.IO.Path.Combine(projectDirectory, "Resources", "Data", "finds.json");

                        if (File.Exists(resultsFilePath))
                        {
                            string preJson = File.ReadAllText(resultsFilePath);

                            // Check if the file is empty
                            if (!string.IsNullOrWhiteSpace(preJson))
                            {
                                var findsData = JsonConvert.DeserializeObject<List<AnalysisResult>>(preJson);
                                DisplayAnimalData(findsData);
                            }
                        }


                    // implement then creation of the slots in the scroll canvas for both
                }

                userInfo = NamePage.LoadOldUserInfoData();
                if (userInfo != null)
                {
                    //Debug.WriteLine((string)userInfo["Name"]);
                    if (userInfo.ContainsKey("Name"))
                        Nametag.Text = (string)userInfo["Name"];
                    if (userInfo.ContainsKey("Stars"))
                        Starstag.Text = userInfo["Stars"].ToString() + " Stars";
                }
            };

            if ((mainWindow != null) && (mainWindow.AreCurtainsIn()))
            {
                mainWindow.SlideCurtains(false, () =>
                {
                    //Debug.WriteLine("slid out");
                });
            }
        }

        // Display Animal Data
        private void DisplayAnimalData(List<AnalysisResult> findsData)
        {
            foreach (var find in findsData)
            {
                // Create a new Canvas for each animal
                Canvas animalCanvas = new Canvas
                {
                    Width = 200,
                    Height = 120,
                    Margin = new Thickness(10),
                };

                // Apply clipping for rounded corners
                animalCanvas.Clip = new RectangleGeometry
                {
                    Rect = new Rect(0, 0, 200, 120),
                    RadiusX = 15,
                    RadiusY = 15
                };

                // Set background gradient
                LinearGradientBrush backgroundBrush = new LinearGradientBrush
                {
                    StartPoint = new Point(0.5, 0),
                    EndPoint = new Point(0.5, 1)
                };
                backgroundBrush.GradientStops.Add(new GradientStop(Colors.Black, 1));
                backgroundBrush.GradientStops.Add(new GradientStop(Color.FromRgb(32, 41, 78), 0));  // #FF20294E
                animalCanvas.Background = backgroundBrush;

                // Image and animal info setup (use actual images)
                Image animalImage = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Animal Images/" + find.Animal + ".png")),
                    Width = 80,
                    Height = 80,
                    Stretch = Stretch.UniformToFill,
                    Margin = new Thickness(8, 17, 0, 0)
                };

                // Apply circular clipping to image
                animalImage.Clip = new EllipseGeometry
                {
                    Center = new Point(40, 40),
                    RadiusX = 40,
                    RadiusY = 40
                };
                animalCanvas.Children.Add(animalImage);

                // Animal name with viewbox for text scaling
                Viewbox nameViewbox = new Viewbox
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Height = 31,
                    Width = 94,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetTop(nameViewbox, 22);
                Canvas.SetLeft(nameViewbox, 101);

                TextBlock nameBlock = new TextBlock
                {
                    Text = find.Animal,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Poor Richard")
                };
                nameViewbox.Child = nameBlock;
                animalCanvas.Children.Add(nameViewbox);

                // Time block with the date and time
                TextBlock timeBlock = new TextBlock
                {
                    Text = $"{find.Date:dd/MM/yyyy}\n{find.Time:HH:mm:ss}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("TI-Nspire"),
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetBottom(timeBlock, -0);
                Canvas.SetRight(timeBlock, 10);
                animalCanvas.Children.Add(timeBlock);

                Image pointerImage = new Image
                {
                    Source = new BitmapImage(new Uri("pack://application:,,,/Resources/Images/Map/Pointer.png")),
                    Width = 43,
                    Height = 42,
                    Stretch = Stretch.UniformToFill,
                };
                Canvas.SetTop(pointerImage, 10);
                Canvas.SetLeft(pointerImage, 163);
                animalCanvas.Children.Add(pointerImage);

                // Animal name with viewbox for text scaling
                Viewbox locationViewbox = new Viewbox
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    Height = 15,
                    Width = 94,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Canvas.SetTop(locationViewbox, 52);
                Canvas.SetLeft(locationViewbox, 101);

                TextBlock locationBlock = new TextBlock
                {
                    Text = find.Location,
                    FontSize = 12,
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center,
                    FontFamily = new FontFamily("Poor Richard")
                };
                locationViewbox.Child = locationBlock;
                animalCanvas.Children.Add(locationViewbox);

                AnimalDataStackPanel.Children.Add(animalCanvas);
            }

            AnimalScrollViewer.Content = AnimalDataStackPanel;
        }

        // Display Water Data
        private void DisplayWaterData(Dictionary<string, Dictionary<string, string>> vData)
        {
            foreach (var kvp in vData)
            {
                string key = kvp.Key;  // Station name
                Dictionary<string, string> visit = kvp.Value;
                string pPH = visit["pH"], pdOx = visit["DissolvedOxygen"];
                if (double.Parse(pPH) == 0)
                    pPH = "--";
                if (double.Parse(pdOx) == 0)
                    pdOx = "--";

                // Create a Border with white outline
                Border waterBorder = new Border
                {
                    Width = 200,
                    Height = 117,
                    BorderBrush = Brushes.White,  // White outline
                    BorderThickness = new Thickness(2),  // Thickness of the outline
                    Margin = new Thickness(10),
                    Background = Brushes.Black  // Background color inside the border
                };

                // Create a canvas for each water data entry
                Canvas waterCanvas = new Canvas
                {
                    Width = 197,
                    Height = 114,
                    Background = Brushes.Black,
                };

                // Add station name in Viewbox with correct font
                Viewbox nameViewbox = new Viewbox
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Width = 185,
                    Height = 22,
                    VerticalAlignment = VerticalAlignment.Top
                };
                Canvas.SetTop(nameViewbox, 7);
                Canvas.SetLeft(nameViewbox, 3);

                TextBlock nameBlock = new TextBlock
                {
                    Text = key ?? "Unnamed Station",
                    FontSize = 16,
                    FontFamily = new FontFamily("Poor Richard"),
                    Foreground = Brushes.White,
                    TextAlignment = TextAlignment.Center
                };
                nameViewbox.Child = nameBlock;
                waterCanvas.Children.Add(nameViewbox);

                // Add pH information
                TextBlock pHBlock = new TextBlock
                {
                    Text = $"pH: {pPH}",
                    FontSize = 12,
                    Foreground = Brushes.White,
                };
                Canvas.SetTop(pHBlock, 33);
                Canvas.SetLeft(pHBlock, 4);
                waterCanvas.Children.Add(pHBlock);

                // Add dissolved oxygen information
                TextBlock oxygenBlock = new TextBlock
                {
                    Text = $"Dissolved Oxygen (mg/L): {pdOx}",
                    FontSize = 9,
                    Foreground = Brushes.White,
                };
                Canvas.SetTop(oxygenBlock, 53);
                Canvas.SetLeft(oxygenBlock, 4);
                waterCanvas.Children.Add(oxygenBlock);

                // Add station number
                TextBlock stationNumberBlock = new TextBlock
                {
                    Text = $"Station Number: {visit["StationNumber"]}",
                    FontSize = 9,
                    Foreground = Brushes.White,
                };
                Canvas.SetTop(stationNumberBlock, 70);
                Canvas.SetLeft(stationNumberBlock, 5);
                waterCanvas.Children.Add(stationNumberBlock);

                // Add time information
                TextBlock timeBlock = new TextBlock
                {
                    Text = $"{visit["Date"]}; {visit["Time"]}",
                    FontSize = 16,
                    Foreground = Brushes.White,
                    FontFamily = new FontFamily("TI-Nspire"),
                    TextAlignment = TextAlignment.Center
                };
                Canvas.SetTop(timeBlock, 95);
                Canvas.SetLeft(timeBlock, 30);
                waterCanvas.Children.Add(timeBlock);

                // Add the canvas to the border (with outline)
                waterBorder.Child = waterCanvas;
                WaterDataStackPanel.Children.Add(waterBorder);
            }

            WaterScrollViewer.Content = WaterDataStackPanel;
        }



        private void GoBack(object sender, RoutedEventArgs e)
            {
                if (mainWindow != null)
                {
                    mainWindow.SlideCurtains(true, () =>
                    {
                        NavigationService.Navigate(new HomePage());
                    });
                }
            }
        }
}
