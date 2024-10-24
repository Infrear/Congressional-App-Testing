using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using LiveCharts;
using LiveCharts.Wpf;

namespace Congressional_App_Test1.Presentation
{
    public partial class StatisticsPage : Page
    {
        public SeriesCollection ChartSeries { get; set; }
        public string[] LabelsX { get; set; }
        public Func<double, string> YFormatter { get; set; }

        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        public StatisticsPage(string stationName, double pH, double dissolvedOxygen, double temperature, string dateTime)
        {
            InitializeComponent();

            // Display station name and datetime
            StationNameTextBlock.Text = "Water Statistics for " + stationName;
            DateTimeTextBlock.Text = dateTime;

            // Ideal values
            double idealPH = 7.0; // neutral
            double idealDissolvedOxygen = 8.0; // mg/L
            double idealTemperature = 15.0; // °C

            // Chart data - actual vs ideal values
            ChartSeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Actual",
                    Values = new ChartValues<double> { temperature, dissolvedOxygen, pH }
                },
                new ColumnSeries
                {
                    Title = "Ideal",
                    Values = new ChartValues<double> { idealTemperature, idealDissolvedOxygen, idealPH }
                }
            };

            // Set the labels for the X-axis
            LabelsX = new[] { "Temperature (°C)", "Dissolved O2 (mg/L)", "pH" };

            // Set up the label formatter for Y-axis
            YFormatter = value => value.ToString("N");

            // Bind the DataContext so the XAML can access the properties
            DataContext = this;
        }

        private void GoBack(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    NavigationService.Navigate(new MapPage());
                });
            }
        }
    }
}
