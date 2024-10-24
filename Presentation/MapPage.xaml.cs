using Congressional_App_Test1.Presentation;
using Congressional_App_Test1.Resources.Classes;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Numerics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;
using Application = System.Windows.Application;

namespace Congressional_App_Test1
{ 
    /// <summary>
    /// Interaction logic for MapPage.xaml
    /// </summary>
    public delegate void LoadStats(string stationName, double pH, double dissolvedOxygen, double temperature, string dateTime);

    public partial class MapPage : Page
    {


        public static event LoadStats OnStatLoad;

        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;
        private Map? _map;
        List<(string, double, double, double, double, double, string)>? locations;

        public MapPage()
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
                HomePage.LoadinTitleText(TitleBlock);
                Task.Run(async () =>
                {
                    locations = await MapData.retrieveWaterData();

                    if (locations != null) {
                        // Since im on a background thread, make sure to update UI elements on the main thread
                        Dispatcher.Invoke(() =>
                        {
                            _map = new WaterQualityMap(MapAndDotsCanvas, mapImage, border, InfoCanvas, locations);

                            // Create dots for each location
                            _map.CreateMarkers();

                            OnStatLoad += Load_Stats;
                        });
                    }
                });
            };
        }

        public static void Invoke_Load(string stationName, double pH, double dissolvedOxygen, double temperature, string dateTime)
        {

            OnStatLoad?.Invoke(stationName, pH, dissolvedOxygen, temperature, dateTime);
        }

        public void Load_Stats(string stationName, double pH, double dissolvedOxygen, double temperature, string dateTime)
        {
            //if (mainWindow != null)
            //{
            //    mainWindow.SlideCurtains(true, () =>
            //    {
                    NavigationService.Navigate(new StatisticsPage(stationName, pH, dissolvedOxygen, temperature, dateTime));
            //    });
            //}
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

        private async void GoSearch(object sender, RoutedEventArgs e)
        {
            if (_map != null)
            {
                string txt = Regex.Replace(Regex.Unescape(InputTextBox.Text), @"\t|\n|\r", "");
                Vector2? coords = await MapData.GetCoordinates(txt, "AIzaSyCvJhbW_CyUZ3M7KCMfSAhvNwyNFZrgcE4");
                Debug.Print(coords.ToString());

                MonitoringData? closest = MapData.FindClosestStation((Vector2) coords);
                //var (x, y) = _map.ConvertLatLonToPixels(closest.LatLong.X, closest.LatLong.Y)
                //Point p = new Point(x, y);

                Debug.Print(closest.StationName);
                if (closest != null && _map.dots.ContainsKey(closest.StationName))
                {
                   (Ellipse dot, Point rpos, Point tpoint, double dissolvedOxy, double pH) = _map.dots[closest.StationName];
                    _map.ZoomToPoint(rpos, 10, 1000, true, dot, dissolvedOxy, pH, tpoint, txt, closest.StationNumber);
                }
            }
        }
    }
}
