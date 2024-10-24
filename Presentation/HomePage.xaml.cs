using Congressional_App_Test1.Presentation;
using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Windows.Media;
using System.Windows.Navigation;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.IO;

namespace Congressional_App_Test1
{
    /// <summary>
    /// Interaction logic for HomePage.xaml
    /// </summary>
    public class DirsData
    {
        [JsonProperty("Habitats Dirs")]
        public List<string> HabitatsDirs { get; set; }

        [JsonProperty("Water Dirs")]
        public int WaterDirs { get; set; }

        [JsonProperty("Rarity Stars")]
        public Dictionary<string, int> RarityStars { get; set; }
    }

    public partial class HomePage : Page
    {
        // Get a reference to the main window
        MainWindow? mainWindow = Application.Current.MainWindow as MainWindow;

        string projectDirectory = AppDomain.CurrentDomain.BaseDirectory;
        public static DirsData dirsData;
        private static string resultsFilePath;

        public static string FirstCharToUpper(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }
            return $"{char.ToUpper(input[0])}{input[1..]}";
        }

        public HomePage()
        {
            resultsFilePath = Path.Combine(projectDirectory, "Resources", "Data", "Directions.json");

            string preJson2 = File.ReadAllText(resultsFilePath);

            // Deserialize the JSON into the class
            dirsData = JsonConvert.DeserializeObject<DirsData>(preJson2);
            Debug.WriteLine(dirsData);
            if ((mainWindow != null) && (mainWindow.AreCurtainsIn()))
            {
                mainWindow.SlideCurtains(false, () =>
                {
                    //Debug.WriteLine("slid out");
                });
            }
            InitializeComponent();
        }

        // Event handler for all buttons
        private void GoToMapPage_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    // Navigate to the MapPage after the animation
                    NavigationService.Navigate(new MapPage());

                    Debug.WriteLine("slid in");
                });
            }
        }

        // Event handler for all buttons
        private void GoToMap2Page_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    NavigationService.Navigate(new MapPage2());
                });
            }
        }


        // Event handler for all buttons
        private void GoToMap3Page_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    NavigationService.Navigate(new CameraPage());
                });
            }
        }


        // Event handler for all buttons
        private void GoToMap4Page_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    NavigationService.Navigate(new About());
                });
            }
        }


        // Event handler for all buttons
        private void GoToMap5Page_Click(object sender, RoutedEventArgs e)
        {
            if (mainWindow != null)
            {
                mainWindow.SlideCurtains(true, () =>
                {
                    NavigationService.Navigate(new Collection());
                });
            }
        }

        // method for loading in title text
        public static void LoadinTitleText(TextBlock TitleBlock)
        {
            Debug.Print("randis");

            var opacAnim = new DoubleAnimation
            {
                From = 1,
                To = 0,
                Duration = new Duration(TimeSpan.FromSeconds(3))
            };
            Storyboard.SetTarget(opacAnim, TitleBlock);
            Storyboard.SetTargetProperty(opacAnim, new PropertyPath(UIElement.OpacityProperty));


            // Create the TranslateTransform for Y
            TranslateTransform translateTransform = new TranslateTransform();
            TitleBlock.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(),   // Add other transforms if needed
                    new RotateTransform(),
                    new SkewTransform(),
                    translateTransform     
                }
            };


            // Create the DoubleAnimationUsingKeyFrames
            DoubleAnimationUsingKeyFrames moveAnimation = new DoubleAnimationUsingKeyFrames();

            // Create the EasingDoubleKeyFrame
            EasingDoubleKeyFrame keyFrame = new EasingDoubleKeyFrame
            {
                KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(7)),
                Value = -10
            };

            // Apply the easing function
            QuinticEase easingFunction = new QuinticEase { EasingMode = EasingMode.EaseOut };
            keyFrame.EasingFunction = easingFunction;

            // Add the keyframe to the animation
            moveAnimation.KeyFrames.Add(keyFrame);

            // Set the target property and target for the animation
            Storyboard.SetTarget(moveAnimation, TitleBlock);
            Storyboard.SetTargetProperty(moveAnimation, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.Y)"));


            //var moveAnim = new DoubleAnimation
            //{
            //    From = 0,
            //    To = 100,
            //    Duration = new Duration(TimeSpan.FromSeconds(10))
            //};
            //Storyboard.SetTarget(moveAnim, TitleBlock);
            //Storyboard.SetTargetProperty(moveAnim, new PropertyPath(TranslateTransform.YProperty));



            string txt = TitleBlock.Text;

            Storyboard tstory = new Storyboard(), mstory = new Storyboard();
            tstory.FillBehavior = FillBehavior.HoldEnd;

            DiscreteStringKeyFrame discreteStringKeyFrame;
            StringAnimationUsingKeyFrames stringAnimationUsingKeyFrames = new StringAnimationUsingKeyFrames();
            stringAnimationUsingKeyFrames.Duration = new Duration(TimeSpan.FromSeconds(3));

            string tmp = string.Empty;
            foreach (char c in txt)
            {
                discreteStringKeyFrame = new DiscreteStringKeyFrame();
                discreteStringKeyFrame.KeyTime = KeyTime.Paced;
                tmp += c;
                discreteStringKeyFrame.Value = tmp;
                stringAnimationUsingKeyFrames.KeyFrames.Add(discreteStringKeyFrame);
            }
            Storyboard.SetTargetName(stringAnimationUsingKeyFrames, TitleBlock.Name);
            Storyboard.SetTargetProperty(stringAnimationUsingKeyFrames, new PropertyPath(TextBlock.TextProperty));
            tstory.Children.Add(stringAnimationUsingKeyFrames);

            mstory.Children.Add(opacAnim);
            mstory.Children.Add(moveAnimation);

            Debug.Print("Starting typing animation...");
            tstory.Completed += (s, e) =>
            {
                Debug.Print("Typing animation completed. Starting fade out.");
                mstory.Begin();
            };
            tstory.Begin(TitleBlock);
        }
    }
}
