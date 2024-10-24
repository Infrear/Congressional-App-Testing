using Congressional_App_Test1.Utilities;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Xaml.Schema;
using Path = System.IO.Path;
using File = System.IO.File;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Net.Http;
using Newtonsoft.Json.Linq;

namespace Congressional_App_Test1.Resources.Classes
{
    public class AnimalCoordInfo
    {
        public double[] BoundingBox { get; set; }
    }



    public static class MapData
    {
        static string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;
        public static int max = 20;

        public static List<MonitoringData> monitoringDataList;
        public static Dictionary<string, AnimalCoordInfo> animalcoordInfo;
        // Static constructor to initialize static fields
        static MapData()
        {
            monitoringDataList = new List<MonitoringData>();
        }

        static Random random = new Random();
        // private static List<(string name, double latitude, double longitude, string waterQuality, double pH)> locations = new List<(string, double, double, string, double)>{};

        // Latitude: 32.3182314, Longitude: -86.902298 (TERRAPIN CREEK AT ELLISVILLE AL)
        // Latitude 42.629525, Longitude: -71.370652 (My house)
        // Latitude 42.621368, Longitude: -71.375674 (school)

        private static List<(string name, double latitude, double longitude, double dissolvedOxy, double pH, double temperature, string dateTime)> locations = new List<(string, double, double, double, double, double, string)>
        {
            ("TERRAPIN CREEK AT ELLISVILLE AL", 32.3182314, -86.902298, 0, 8.2, 15.5, "9/15"),
            //("My House", 27.664129972169594, -100.39973895717573, 6.9, 6.4, 58, "9/16"),
            ("STATION TEST 05", 42.621368, -71.375674, 7.3, 7.8, 63, "9/17"),
            ("KLAMATH RIVER BLW JOHN C.BOYLE PWRPLNT, NR KENO,OR", 42.12587, -121.930338, 7.1, 5.9, 60, "9/14"),
            ("CHATTAHOOCHEE R .36 MI DS WFG DAM NR FT GAINES, GA", 31.609057, -85.04715, 8.1, 0, 21.7, "9/18"),
            ("MISSOURI RIVER AT KANSAS CITY, MO", 39.099728, -94.578568, 7.5, 6.8, 62, "9/19"),
            ("OHIO RIVER AT CINCINNATI, OH", 39.103118, -84.512020, 6.7, 7.0, 64, "9/20"),
            ("HUDSON RIVER AT TROY, NY", 42.728412, -73.691785, 8.0, 6.1, 60, "9/21"),
            ("SACRAMENTO RIVER AT SACRAMENTO, CA", 38.581573, -121.494400, 7.2, 7.5, 61, "9/22"),
            ("COLORADO RIVER AT AUSTIN, TX", 30.267153, -97.743057, 7.8, 6.3, 65, "9/23"),
            ("COLUMBIA RIVER AT PORTLAND, OR", 45.505106, -122.675026, 7.0, 6.9, 64, "9/24"),
            ("TENNESSEE RIVER AT KNOXVILLE, TN", 35.960638, -83.920739, 6.8, 7.2, 66, "9/25"),
            ("RED RIVER AT SHREVEPORT, LA", 32.525152, -93.750179, 7.4, 6.6, 60, "9/26"),
            ("MISSISSIPPI RIVER AT MEMPHIS, TN", 35.149532, -90.048981, 7.3, 7.0, 61, "9/27"),
            ("RIO GRANDE AT EL PASO, TX", 31.761878, -106.485022, 6.9, 6.5, 63, "9/28")
        };

        private static List<(string speciesName, Rect boundingBox)> habitats = new List<(string, Rect)> { };


        readonly static string gkey = "AIzaSyCvJhbW_CyUZ3M7KCMfSAhvNwyNFZrgcE4";

        // WATER DATA
        private static double Haversine(double lat1, double lon1, double lat2, double lon2)
        {
            //Debug.WriteLine(lat2);
            const double R = 6371; // Radius of the Earth in kilometers
            var dLat = (lat2 - lat1) * (Math.PI / 180);
            var dLon = (lon2 - lon1) * (Math.PI / 180);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(lat1 * (Math.PI / 180)) * Math.Cos(lat2 * (Math.PI / 180)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return R * c; // Distance in kilometers
        }

        private static Vector2 GenerateRandomVector2()
        {
            // Adjusted Latitude range
            double northernmost = 43.0; // Slightly decreased to focus on land
            double southernmost = 20.0;  // Slightly increased to avoid water areas

            // Adjusted Longitude range
            double easternmost = -60.5;  // Slightly decreased to focus more on land
            double westernmost = -140.0;  // Slightly increased to avoid water areas

            // Generate random latitude and longitude within the bounds
            double latitude = random.NextDouble() * (northernmost - southernmost) + southernmost;
            double longitude = random.NextDouble() * (easternmost - westernmost) + westernmost;

            // Return as Vector2 where X = Latitude, Y = Longitude
            return new Vector2((float)latitude, (float)longitude);
        }

        public static MonitoringData? FindClosestStation(Vector2 userLatLon, List<MonitoringData>? stations = null)
        {
            stations ??= monitoringDataList; // Use monitoringDataList if no stations are provided
            //Debug.Print(stations.Count.ToString());
            MonitoringData? closestStation = null;
            double closestDistance = double.MaxValue;

            foreach (var station in stations)
            {
                double stationLat = station.LatLong.X;
                double stationLon = station.LatLong.Y;

                double distance = Haversine(userLatLon.X, userLatLon.Y, stationLat, stationLon);

                //Debug.WriteLine(station.StationName + "\n dist is " + distance);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestStation = station;
                }
            }

            return closestStation;
        }

        public static async Task<Vector2?> GetCoordinates(string location, string apiKey)
        {
            using (HttpClient client = new HttpClient())
            {
                string url = $"https://maps.googleapis.com/maps/api/geocode/json?address={Uri.EscapeDataString(location)}&key={apiKey}";
                HttpResponseMessage response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    var jsonObject = JObject.Parse(jsonResponse);

                    // Check if results are found
                    if (jsonObject["results"].HasValues)
                    {
                        double latitude = (double)jsonObject["results"][0]["geometry"]["location"]["lat"];
                        double longitude = (double)jsonObject["results"][0]["geometry"]["location"]["lng"];
                        return new Vector2((float)latitude, (float)longitude);
                    }
                }
                else
                {
                    Console.WriteLine($"Error: {response.StatusCode}");
                }
            }
            return null; // Location not found

            //Debug.WriteLine(location);
            if (location == "TERRAPIN CREEK AT ELLISVILLE AL")
            {
                //Debug.WriteLine("1");
                return new Vector2(32.3182314F, -86.902298F);
            }//

            else if (location == "KLAMATH RIVER BLW JOHN C.BOYLE PWRPLNT, NR KENO,OR")
                return new Vector2(42.12587F, -121.930338F);
            else
            {
                //Debug.WriteLine("3");
                return GenerateRandomVector2();
                //return new Vector2(9.056265F, 7.498526F); // abuja, nigeria
            }
        }

        public static async Task<List<(string name, double latitude, double longitude, double dissolvedOxygen, double pH, double temperature, string dateTime)>?> retrieveWaterData()
        {
            //if (locations.Count > 0)
            //    return locations;

            var htmlUrl = "https://waterdata.usgs.gov/nwis/current/?type=quality";
            HtmlWeb web = new HtmlWeb();
            var htmlDoc = web.Load(htmlUrl);

            // Select all the table rows that contain monitoring data
            var rows = htmlDoc.DocumentNode.SelectNodes("//tr[@align='right']");

            // Create a list to store monitoring data
            monitoringDataList = new List<MonitoringData>();
            locations = new List<(string name, double latitude, double longitude, double dissolvedOxy, double pH, double temperature, string dateTime)>();
            if (rows != null)
            {
                foreach (var row in rows)
                {
                    var cells = row.SelectNodes("td");

                    if (cells != null && cells.Count >= 7)
                    {
                        if (locations.Count < max)
                        {
                            int r = random.Next(1, 11);
                            var stationName = cells[1].InnerText.Trim().Replace("&nbsp;", "");
                            var pH = cells[5].InnerText.Trim().Replace("&nbsp;", "");
                            var dissolvedOxygen = cells[4].InnerText.Trim().Replace("&nbsp;", "");
                            if ((r < 5 || (stationName == "CHATTAHOOCHEE R .36 MI DS WFG DAM NR FT GAINES, GA"))  && (double.TryParse(pH, out double fpH) || double.TryParse(dissolvedOxygen, out double fdOx)));
                            {
                                var stationNumber = cells[0].InnerText.Trim().Replace("&nbsp;", "");
                                //Debug.WriteLine(stationNumber);
                                var specificConductance = cells[2].InnerText.Trim().Replace("&nbsp;", "");
                                var dateTime = cells[6].InnerText.Trim().Replace("&nbsp;", "");
                                var latLong = (Vector2)await GetCoordinates(stationName, gkey);
                                var temperature = cells[3].InnerText.Trim().Replace("&nbsp;", "");

                                // Create a new MonitoringData object and add it to the list
                                var monitoringData = new MonitoringData
                                {
                                    StationNumber = stationNumber,
                                    StationName = stationName,
                                    SpecificConductance = specificConductance,
                                    Temperature = temperature,
                                    DissolvedOxygen = dissolvedOxygen,
                                    LatLong = latLong,
                                    pH = pH,
                                    DateTime = dateTime
                                };
                                monitoringDataList.Add(monitoringData);
                                //Debug.WriteLine(temperature);
                                //Debug.WriteLine(dissolvedOxygen);
                                //Debug.WriteLine(pH);

                                // Try to parse the values, set to 0 if parsing fails
                                double.TryParse(temperature, out double fTemp);
                                double.TryParse(dissolvedOxygen, out double ffdOx);
                                double.TryParse(pH, out double ffpH);

                                // Add data to locations (even if defaults are used)
                                locations.Add((stationName, latLong.X, latLong.Y, ffdOx, ffpH, fTemp, dateTime));
                            }
                        }
                    }
                }

                // Output the extracted monitoring data
                //foreach (var data in monitoringDataList)
                //{
                //    Debug.WriteLine($"Station Number: {data.StationNumber}, " +
                //                    $"Station Name: {data.StationName}, " +
                //                    $"Specific Conductance: {data.SpecificConductance}, " +
                //                    $"Temperature: {data.Temperature}, " +
                //                    $"Dissolved Oxygen: {data.DissolvedOxygen}, " +
                //                    $"pH: {data.pH}, " +
                //                    $"Date/Time: {data.DateTime}");
                //}
            }
            else
            {
                Debug.WriteLine("No monitoring data rows found.");
            }

            /*
            string location = "TERRAPIN CREEK AT ELLISVILLE AL";
            var coordinates = await GetCoordinates(location, gkey);


            //if (coordinates is Vector2 coords)
            if (coordinates != null)
            {
                Debug.WriteLine($"Latitude: {coordinates.Value.X}, Longitude: {coordinates.Value.Y}");
            }
            else
            {
                Debug.WriteLine("Location not found.");
            }
            */


            var found = FindClosestStation(new Vector2(42.621368F, -71.375674F), monitoringDataList); // School
            if (found != null)
            {
                Debug.WriteLine("closest is " + found.StationName);
                Debug.WriteLine("pos is (Latitude: " + found.LatLong.X + ", Longitude: " + found.LatLong.Y);
            }


            return locations;
        }

        public static async Task<List<(string speciesName, Rect boundingBox)>?> retrieveHabitatData()
        {
            AnimalCoordInfo model = new AnimalCoordInfo { BoundingBox = new double[] { -84.58762883256291, 35.55268582396782, -83.77402467973586, 36.120937088892745 } };

            var animalcoordInfo = new Dictionary<string, AnimalCoordInfo>
{
    { "Pickerel Frog - Lithobates palustris", new AnimalCoordInfo { BoundingBox = new double[] { -97.56012970593791, 28.38933563690945, -66.9017614484064, 48.30415958023948 } } },

    { "Belted Kingfisher - Megaceryle alcyon", new AnimalCoordInfo { BoundingBox = new double[] { -124.82920597520852, 24.464615448485446, -66.9017614484064, 49.384366329261844 } } },

    { "Bobcat - Lynx rufus", new AnimalCoordInfo { BoundingBox = new double[] { -124.82920597520851, 24.969082727922753, -66.9017614484064, 49.384366329261844 } } },
    { "Northern Flying Squirrel - Glaucomys sabrinus", new AnimalCoordInfo { BoundingBox = new double[] { -124.82920597520851, 33.553269440284105, -66.9017614484064, 49.384366329261844 } } },

    { "Texas Banded Gecko - Coleonyx brevis", new AnimalCoordInfo { BoundingBox = new double[] { -107.28102732433629, 25.837381551691927, -97.09610499647938, 33.82608050007801 } } }
};

            // Initialize habitats list
            var habitats = new List<(string, Rect)>();

            // Add the Berry Cave Salamander (existing animal) to the habitats
            habitats.Add(("Berry Cave Salamander", new Rect(new Point(model.BoundingBox[1], model.BoundingBox[0]), new Point(model.BoundingBox[3], model.BoundingBox[2]))));

            // Iterate through the animalcoordInfo dictionary
            foreach (var entry in animalcoordInfo)
            {
                string animalName = entry.Key;
                double[] boundingBox = entry.Value.BoundingBox;

                // Create the rectangle using the values from the bounding box
                Rect rect = new Rect(
                    new Point(boundingBox[1], boundingBox[0]),  // Top-left corner (latitude, longitude)
                    new Point(boundingBox[3], boundingBox[2])   // Bottom-right corner (latitude, longitude)
                );

                // Add the animal's name and corresponding rectangle to the habitats list
                habitats.Add((animalName, rect));
            }

            return habitats;

        }
    }
    public class MonitoringData
    {
        public string? StationNumber { get; set; }
        public string? StationName { get; set; }
        public string? SpecificConductance { get; set; }
        public string? Temperature { get; set; }
        public string? DissolvedOxygen { get; set; }
        public Vector2 LatLong { get; set; }
        public string? pH { get; set; }
        public string? DateTime { get; set; }
    }
}
