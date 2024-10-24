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
using Path = System.IO.Path;
using File = System.IO.File;
using Newtonsoft.Json;
using static OrganismHabitatMap;
using System.Text.RegularExpressions;
using Windows.UI.Popups;

namespace Congressional_App_Test1.Presentation
{
    /// <summary>
    /// Interaction logic for NamePage.xaml
    /// </summary>
    public partial class NamePage : Page
    {
        // Load animal information from the JSON file into a dictionary
        Dictionary<string, dynamic>? userInfo;
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;

        // Get a reference to the main window
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        public NamePage()
        {
            resultsFilePath = Path.Combine(projectDirectory, "Resources", "Data", "baseUserData.json");

            userInfo = LoadOldUserInfoData();
            if (userInfo != null && userInfo.ContainsKey("Name"))
            {
                Debug.WriteLine((string) userInfo["Name"]);
            }
            InitializeComponent();
        }

        public static Dictionary<string, dynamic>? LoadOldUserInfoData()
        {
            if (File.Exists(resultsFilePath))
            {
                string json = File.ReadAllText(resultsFilePath);

                Debug.WriteLine(json);

                // Check if the file is not empty
                if (!string.IsNullOrWhiteSpace(json))
                {
                    // Deserialize the JSON into a dictionary
                    return JsonConvert.DeserializeObject<Dictionary<string, dynamic>>(json);
                }
            }
            return null;
        }

        private void onClick(object sender, RoutedEventArgs e)
        {

            if (File.Exists(resultsFilePath))
            {
                Debug.WriteLine("2");
                // Load existing data if the file exists
                Dictionary<string, dynamic>? existingData = LoadOldUserInfoData();

                string preJson = File.ReadAllText(resultsFilePath);
                Debug.WriteLine(preJson);
                
                // For now we wont load any data
                if (true || existingData != null && string.IsNullOrWhiteSpace(existingData["Name"]))
                {
                    string rt = new TextRange(tb.Document.ContentStart, tb.Document.ContentEnd).Text;
                    existingData["Name"] = Regex.Replace(Regex.Unescape(rt), @"\t|\n|\r", "");
                    Debug.WriteLine("3");

                    string json = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                    File.WriteAllText(resultsFilePath, json);
                }
            }
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
