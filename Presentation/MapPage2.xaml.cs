using Congressional_App_Test1.Resources.Classes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Numerics;

using Path = System.IO.Path;
using File = System.IO.File;
using Newtonsoft.Json;

namespace Congressional_App_Test1.Presentation
{
    /// <summary>
    /// Interaction logic for MapPage2.xaml
    /// </summary>
    public partial class MapPage2 : Page
    {

        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        private Map? _map;
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;

        public MapPage2()
        {
            InitializeComponent();

            if ((mainWindow != null) && (mainWindow.AreCurtainsIn()))
            {
                mainWindow.SlideCurtains(false, () =>
                {
                    Debug.WriteLine("slid out");
                });
            }
            
            Loaded += (s, e) =>
            {
                //animalIcon1.Source = new BitmapImage(new Uri("https://images.squarespace-cdn.com/content/5dec545960df274331b569f2/1633992311860-XTIJPRLMMFODS8HL0NA2/image-asset.jpeg?format=1500w&content-type=image%2Fjpeg"));

                HomePage.LoadinTitleText(TitleBlock);
                Task.Run(async () =>
                {
                    List<(string speciesName, double latitude, double longitude)> species = new List<(string, double, double)>();
                    /* ("Toad", 27.664129972169594, -100.39973895717573),
                                ("test2", 50.18498527008573, -65.86831977256452)
                                } // Pass in species locations*/
                    
                    // animal data
                    resultsFilePath = System.IO.Path.Combine(projectDirectory, "Resources", "Data", "finds.json");

                    if (File.Exists(resultsFilePath))
                    {
                        string preJson = File.ReadAllText(resultsFilePath);

                        // Check if the file is empty
                        if (!string.IsNullOrWhiteSpace(preJson))
                        {
                            var findsData = JsonConvert.DeserializeObject<List<AnalysisResult>>(preJson);
                            foreach (var find in findsData)
                            {
                                Vector2? aCoords = await MapData.GetCoordinates(find.Location, "AIzaSyCvJhbW_CyUZ3M7KCMfSAhvNwyNFZrgcE4");
                                if (aCoords != null)
                                {
                                    species.Add((find.Animal, ((Vector2)aCoords).X, ((Vector2)aCoords).Y));
                                }
                            }
                        }
                    }

                               
                    // Sample data for habitats
                    var habitats = await MapData.retrieveHabitatData();

                    if (habitats != null) {
                        // background thread, update UI elements on the main thread
                        Dispatcher.Invoke(() =>
                        {

                            // Create OrganismHabitatMap instance
                            _map = new OrganismHabitatMap(
                                MapAndDotsCanvas,
                                mapImage,
                                border,
                                InfoCanvas,
                                habitats,
                                species
                            );

                            // Create markers for each habitat and location
                            _map.CreateMarkers();
                        });
                    }
                });
            };
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
