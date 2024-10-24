using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Congressional_App_Test1.Presentation
{
    /// <summary>
    /// Interaction logic for Collection.xaml
    /// </summary>
    public partial class Collection : Page
    {
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        // File path for saving results
        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        private static string resultsFilePath;

        // Load animal information from the JSON file into a dictionary
        Dictionary<string, AnimalInfo> animalInfo;

        public Collection()
        {
            if ((mainWindow != null) && (mainWindow.AreCurtainsIn()))
            {
                mainWindow.SlideCurtains(false, () =>
                {
                    //Debug.WriteLine("slid out");
                });
            }
            InitializeComponent();

            string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
            string resultsFilePath = System.IO.Path.Combine(projectDirectory, "Resources", "Data", "baseAnimalData.json");

            if (File.Exists(resultsFilePath))
            {
                string json = File.ReadAllText(resultsFilePath);
                if (!string.IsNullOrWhiteSpace(json))
                {
                    animalInfo = JsonConvert.DeserializeObject<Dictionary<string, AnimalInfo>>(json);
                    PopulateGrid();
                }
            }
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

        // Method to dynamically populate the grid
        private void PopulateGrid()
        {
            int row = 0, col = 0;
            int maxCols = 2; // Max number of columns before starting a new row

            // Clear existing children and definitions in case it's populated multiple times
            MainGrid.Children.Clear();
            MainGrid.RowDefinitions.Clear();
            MainGrid.ColumnDefinitions.Clear();

            // Create necessary column definitions
            for (int i = 0; i <= maxCols; i++)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition());
            }

            foreach (var entry in animalInfo)
            {
                string animalName = entry.Key;
                AnimalInfo info = entry.Value;

                // Create a Border for each animal slot
                Border animalSlot = new Border
                {
                    Width = 115,
                    Height = 160,
                    Background = Brushes.White,
                    BorderBrush = Brushes.LightGray,
                    BorderThickness = new Thickness(2),
                    CornerRadius = new CornerRadius(8),
                    Padding = new Thickness(10),
                    Margin = new Thickness(0)
                };

                // Create StackPanel to hold the image and details
                StackPanel stackPanel = new StackPanel
                {
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Create Image for the animal
                Image animalImage = new Image
                {
                    Width = 80,
                    Height = 80,
                    Source = new BitmapImage(new Uri(info.ImageUriSource, UriKind.RelativeOrAbsolute)),
                    Stretch = System.Windows.Media.Stretch.UniformToFill
                };
                animalImage.Clip = new EllipseGeometry { Center = new Point(40, 40), RadiusX = 40, RadiusY = 40 };

                // Create TextBlocks for animal name and rarity
                TextBlock nameBlock = new TextBlock
                {
                    Text = animalName,
                    FontSize = 14,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.DarkSlateGray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                TextBlock rarityBlock = new TextBlock
                {
                    Text = info.Rarity,
                    FontSize = 12,
                    Foreground = Brushes.Gray,
                    HorizontalAlignment = HorizontalAlignment.Center
                };

                // Add elements to stack panel
                stackPanel.Children.Add(animalImage);
                stackPanel.Children.Add(nameBlock);
                stackPanel.Children.Add(rarityBlock);

                // Add stack panel to border
                animalSlot.Child = stackPanel;

                // Add hover event to update the display
                animalSlot.MouseEnter += (s, e) => UpdateDisplay(animalName, info);

                // Add the border to the grid
                Grid.SetRow(animalSlot, row);
                Grid.SetColumn(animalSlot, col);
                MainGrid.Children.Add(animalSlot);

                // Manage rows and columns for grid
                col++;
                if (col > maxCols)  // Move to next row after max columns
                {
                    col = 0;
                    row++;
                    MainGrid.RowDefinitions.Add(new RowDefinition()); // Add a new row for each new line of animals
                }
            }

            // Ensure at least one row is defined if no rows are added dynamically
            if (MainGrid.RowDefinitions.Count == 0)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition());
            }
        }


        // Method to update the display image, name, and habitat description when hovering over an animal
        private void UpdateDisplay(string animalName, AnimalInfo animal)
        {
            // Update the display image
            DisplayImage.Source = new BitmapImage(new Uri(animal.ImageUriSource, UriKind.RelativeOrAbsolute));

            // Update the name and habitat description text blocks
            nameblock1.Text = animalName;
            habitatDescriptionBlock.Text = $"Rarity: {animal.Rarity}\n--\nHabitat: {animal.HabitatDescription}";
        }
    }
}
