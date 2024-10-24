using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Emgu.CV;
using Emgu.CV.Structure;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision;
using Microsoft.Azure.CognitiveServices.Vision.ComputerVision.Models;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;
using System.Windows.Documents;
using System.Configuration; // For JSON serialization

namespace Congressional_App_Test1.Presentation
{
    public class AnalysisResult
    {
        public string Location { get; set; }
        public string Animal { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
    }

    public partial class CameraPage : Page
    {
        public int take = 1;
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        private CancellationTokenSource? _webcamCancellationTokenSource;
        private Mat _currentFrame; // Store the current frame for capturing
        static List<List<(string Name, double Confidence)>> testResults = new List<List<(string Name, double Confidence)>> 
        {
            new List<(string Name, double Confidence)>
            {
                ("person", 99.74),
                ("human face", 99.04),
                ("smile", 98.43),
                ("glasses", 97.21),
                ("wall", 95.02),
                ("indoor", 94.41),
                ("cool", 89.16),
                ("clothing", 88.95),
                ("black hair", 88.54),
                ("eyewear", 86.54),
                ("vision care", 85.61),
                ("woman", 66.83),
            },
            new List<(string Tag, double Confidence)>
            {
                ("animal", 99.47),
                ("mammal", 98.99),
                ("grass", 95.89),
                ("outdoor", 93.86),
                ("lion", 93.40),
                ("terrestrial animal", 90.35),
                ("wildlife", 88.40),
                ("big cats", 87.47),
                ("safari", 84.38),
                ("field", 59.49),
                ("flat", 55.63)
            }
        };

        // File path for saving results
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath, resultsFilePath2, resultsFilePath3; // 

        // Load animal information from the JSON file into a dictionary
        Dictionary<string, AnimalInfo> animalInfo;
        string? currentAnimal;

        // Computer Vision key and endpoint
        //static string key = "fae7e3078aca4f92ad2ebdb72a557e1d";
        //static string endpoint = "https://azzyimgai.cognitiveservices.azure.com/";

        public CameraPage()
        {
            if ((mainWindow != null) && (mainWindow.AreCurtainsIn()))
            {
                mainWindow.SlideCurtains(false, () =>
                {
                    //Debug.WriteLine("slid out");
                });
            }
            InitializeComponent();
            resultsFilePath = Path.Combine(projectDirectory, "Resources", "Data", "finds.json");
            resultsFilePath2 = Path.Combine(projectDirectory, "Resources", "Data", "baseUserData.json");
            resultsFilePath3 = Path.Combine(projectDirectory, "Resources", "Data", "baseAnimalData.json");


            if (File.Exists(resultsFilePath3))
            {
                string json = File.ReadAllText(resultsFilePath3);

                // Check if the file is not empty
                if (!string.IsNullOrWhiteSpace(json))
                {
                    // Deserialize the JSON into a dictionary
                    animalInfo = JsonConvert.DeserializeObject<Dictionary<string, AnimalInfo>>(json);
                }
            }
        }

        private async Task ProcessWebcam(CancellationToken cancellationToken)
        {
            using VideoCapture capture = new VideoCapture();
            _currentFrame = new Mat(); // Initialize current frame

            while (!cancellationToken.IsCancellationRequested)
            {
                capture.Read(_currentFrame);

                if (!_currentFrame.IsEmpty)
                {
                    await Dispatcher.InvokeAsync(() =>
                    {
                        WebCamImage.Source = ConvertMatToBitmapSource(_currentFrame);
                    });
                }

                await Task.Delay(30); // Small delay to avoid overwhelming CPU
            }
        }

        private BitmapSource ConvertMatToBitmapSource(Mat mat)
        {
            using var bitmap = mat.ToBitmap(); // Convert Mat to System.Drawing.Bitmap
            var bitmapSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                bitmap.GetHbitmap(),
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            return bitmapSource;
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
        {
            if (_webcamCancellationTokenSource == null)
            {
                ab.Visibility = Visibility.Visible;

                _webcamCancellationTokenSource = new CancellationTokenSource();
                Task.Run(() => ProcessWebcam(_webcamCancellationTokenSource.Token));
            }
        }

        private void StopButton_Click(object sender, RoutedEventArgs e)
        {
            _webcamCancellationTokenSource?.Cancel();
            _webcamCancellationTokenSource = null;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            _webcamCancellationTokenSource?.Cancel();
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentFrame != null && !_currentFrame.IsEmpty)
            {
                //string downloadsPath = @"C:\Users\obfra\Downloads\";
                string fileName = "goat";//Path.Combine(downloadsPath, $"captured_image_{DateTime.Now:yyyyMMdd_HHmmss}.png");

                //using var bitmap = _currentFrame.ToBitmap();
                //bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);

                //MessageBox.Show($"Image saved to: {fileName}", "Capture Successful", MessageBoxButton.OK, MessageBoxImage.Information);

                // Analyze the captured image
                await AnalyzeCapturedImageAsync(fileName);
            }
            else
            {
                MessageBox.Show("No image to capture.", "Capture Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        // analyze the image using Azure Cognitive Services
        private async Task AnalyzeCapturedImageAsync(string imagePath)
        {
            try
            {
                // Authenticate and create the client
                //ComputerVisionClient client = Authenticate(endpoint, key);

                // Creating a list that defines the features to be extracted from the image
                //List<VisualFeatureTypes?> features = new List<VisualFeatureTypes?>()
                //{
                //    VisualFeatureTypes.Tags
                //};

                // Read image as a stream for analysis
                //using (Stream imageStream = File.OpenRead(imagePath))
                //{
                    // Analyze the image
                    //ImageAnalysis results = await client.AnalyzeImageInStreamAsync(imageStream, visualFeatures: features);
                    var rTags = testResults[1];//results.Tags;
                    // Display the results
                    if (rTags != null)
                    {
                        string tags = string.Join(", ", rTags.Select(tag => $"{tag.Name} ({tag.Confidence:P})"));
                        //MessageBox.Show($"Tags: {tags}", "Analysis Results", MessageBoxButton.OK, MessageBoxImage.Information);
                        Debug.WriteLine(tags);

                        // Check if any tag matches animals
                        var animals = new[] { "fox", "toad", "owl", "human", "jaguar", "lion" , "elephant"};
                        string detectedAnimal = rTags.Select(tag => tag.Name).FirstOrDefault(tag => animals.Contains(tag));

                        if (detectedAnimal != null)
                        {
                        //Debug.WriteLine(detectedAnimal);
                            currentAnimal = detectedAnimal;
                            vb.Visibility = Visibility.Visible;
                            cb.Visibility = Visibility.Visible;
                            tx.Text = detectedAnimal;
                            tx.Visibility = Visibility.Visible;
                        }
                    }
                    else
                    {
                        //MessageBox.Show("No tags detected in the image.", "Analysis Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                //}
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Analysis failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Authenticate the Computer Vision client
        public static ComputerVisionClient Authenticate(string endpoint, string key)
        {
            ComputerVisionClient client = new ComputerVisionClient(new ApiKeyServiceClientCredentials(key))
            {
                Endpoint = endpoint
            };
            return client;
        }

        // Method to save the analysis result to a file
        private void SaveResult(string location, string _animal)
        {
            string animal = HomePage.FirstCharToUpper(_animal);
            Debug.WriteLine("1");
            if (File.Exists(resultsFilePath))
            {
                Debug.WriteLine("2");
                // Load existing data if the file exists
                List<AnalysisResult> existingResults = LoadResults();

                // Add new result
                existingResults.Add(new AnalysisResult { Location = location, Animal = animal, Date = DateTime.Now.ToString("dd/MM/yyyy"), Time = DateTime.Now.ToString("HH:mm:ss")
                });

                string preJson = File.ReadAllText(resultsFilePath);
                Debug.WriteLine(preJson);

                // Save back to the file
                // Read the file content
                //Debug.WriteLine(existingResults.ToString());
                string json = JsonConvert.SerializeObject(existingResults, Formatting.Indented);
                //Debug.WriteLine(json);
                File.WriteAllText(resultsFilePath, json);

                // Process the jsonContent
                //json = File.ReadAllText(resultsFilePath);
                //Debug.WriteLine(JsonConvert.DeserializeObject<List<AnalysisResult>>(json));
                //Debug.WriteLine(resultsFilePath);
            }

            string rarity = "Common";
            if (animalInfo.ContainsKey(animal))
                rarity = animalInfo[animal].Rarity;

            if (File.Exists(resultsFilePath2))
            {
                Dictionary<string, dynamic>? existingData = NamePage.LoadOldUserInfoData();

                string preJson = File.ReadAllText(resultsFilePath2);

                // Access the star amount for a specific rarity
                int amt = HomePage.dirsData.RarityStars[rarity];

                if (existingData.ContainsKey("Stars"))
                {
                    existingData["Stars"] = ((Int32.Parse(existingData["Stars"])) + amt).ToString(); 
                }
                else
                {
                    Debug.WriteLine("existingData does not contain 'Stars'");
                }

                // Serialize and write the updated data back to the file
                string json = JsonConvert.SerializeObject(existingData, Formatting.Indented);
                File.WriteAllText(resultsFilePath2, json);
            }
        }

        private void Button_Click2(object sender, RoutedEventArgs e)
        {
            if (currentAnimal == null) return;
            vb.Visibility = Visibility.Collapsed;
            cb.Visibility = Visibility.Collapsed;
            tx.Visibility = Visibility.Collapsed;

            string rt = new TextRange(tb.Document.ContentStart, tb.Document.ContentEnd).Text;

            // Save result
            SaveResult(Regex.Replace(Regex.Unescape(rt), @"\t|\n|\r", ""), currentAnimal);
            currentAnimal = null;
        }

        // Method to load results from the file
        private List<AnalysisResult> LoadResults()
        {
            if (File.Exists(resultsFilePath))
            {
                string json = File.ReadAllText(resultsFilePath);

                // Check if the file is empty
                if (!string.IsNullOrWhiteSpace(json))
                {
                    return JsonConvert.DeserializeObject<List<AnalysisResult>>(json);
                }
            }

            // Return an empty list if the file doesn't exist or is empty
            return new List<AnalysisResult>();
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
