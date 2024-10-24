using System;
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Congressional_App_Test1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private EasingDoubleKeyFrame curtainsInKeyFrame = new EasingDoubleKeyFrame
        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(1.5)), // Duration for the animation
            Value = 0, // Target position
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut } // Fast initial speed, dramatic deceleration
        }, curtainsOutKeyFrameL = new EasingDoubleKeyFrame

        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)),
            Value = -400,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
        }, curtainsOutKeyFrameR = new EasingDoubleKeyFrame

        {
            KeyTime = KeyTime.FromTimeSpan(TimeSpan.FromSeconds(2)),
            Value = 400,
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn } // Fast initial speed, dramatic deceleration
        };

        private Storyboard? currentStoryboard; // Track the current animation storyboard
        private bool isAnimationRunning = false; // Track if the animation is currently running

        public MainWindow()
        {
            InitializeComponent();

            // Initialize the left curtain transform
            LeftCurtain.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(),
                    new RotateTransform(),
                    new SkewTransform(),
                    new TranslateTransform(-400, 0), // Start off-screen
                }
            };
            LeftCurtain.Visibility = Visibility.Hidden;

            // Initialize the right curtain transform
            RightCurtain.RenderTransform = new TransformGroup
            {
                Children = new TransformCollection
                {
                    new ScaleTransform(),
                    new RotateTransform(),
                    new SkewTransform(),
                    new TranslateTransform(400, 0), // Start off-screen
                }
            };
            RightCurtain.Visibility = Visibility.Hidden;

            MainFrame.Navigate(new Uri("pack://application:,,,/Presentation/NamePage.xaml", UriKind.Absolute));
            Debug.WriteLine("started");
        }

        public void SlideCurtains(bool slideIn, Action onComplete)
        {
            
            // Stop the current storyboard if it's running
            if (isAnimationRunning && currentStoryboard != null)
            {
                currentStoryboard.Stop();
                isAnimationRunning = false; // Reset the flag if stopping the animation
            }

            // Make the curtains visible if sliding in or already visible
            if (slideIn)
            {
                LeftCurtain.Visibility = Visibility.Visible;
                RightCurtain.Visibility = Visibility.Visible;
            }

            // Create the DoubleAnimationUsingKeyFrames for the left & right curtains
            DoubleAnimationUsingKeyFrames leftSlideAnim = new DoubleAnimationUsingKeyFrames();
            DoubleAnimationUsingKeyFrames rightSlideAnim = new DoubleAnimationUsingKeyFrames();

            // Add the keyframes based on sliding in or out
            if (slideIn)
            {
                leftSlideAnim.KeyFrames.Add(curtainsInKeyFrame);
                rightSlideAnim.KeyFrames.Add(curtainsInKeyFrame);
            }
            else
            {
                leftSlideAnim.KeyFrames.Add(curtainsOutKeyFrameL);
                rightSlideAnim.KeyFrames.Add(curtainsOutKeyFrameR);
            }

            // Set the target property and target for the left animation
            Storyboard.SetTarget(leftSlideAnim, LeftCurtain);
            Storyboard.SetTargetProperty(leftSlideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.X)"));

            // Set the target property and target for the right animation
            Storyboard.SetTarget(rightSlideAnim, RightCurtain);
            Storyboard.SetTargetProperty(rightSlideAnim, new PropertyPath("(UIElement.RenderTransform).(TransformGroup.Children)[3].(TranslateTransform.X)"));

            // Create a new Storyboard to hold the animations
            currentStoryboard = new Storyboard();
            currentStoryboard.Children.Add(leftSlideAnim);
            currentStoryboard.Children.Add(rightSlideAnim);

            Debug.Print("Starting curtain slide animation...");

            currentStoryboard.Completed += (s, e) =>
            {
                isAnimationRunning = false; // Reset the flag on completion

                // Check if the animation was actually running
                if (currentStoryboard.GetCurrentState() != ClockState.Stopped)
                {
                    Debug.Print("Slide animation completed");
                    onComplete?.Invoke(); // Call the callback action (if provided)

                    // Set visibility to hidden after sliding out
                    if (!slideIn)
                    {
                        LeftCurtain.Visibility = Visibility.Hidden;
                        RightCurtain.Visibility = Visibility.Hidden;
                    }
                }
            };

            isAnimationRunning = true; // Set the flag to indicate the animation is running
            currentStoryboard.Begin(); // Start the animation for both curtains
            
            //onComplete?.Invoke(); // Call the callback action (if provided)
        }

        public bool AreCurtainsIn()
        {
            // Return true if an "in" animation is running or if the curtains are invisible (fully in)
            return (currentStoryboard?.GetCurrentState() == ClockState.Active && LeftCurtain.Visibility == Visibility.Visible)
                   || LeftCurtain.Visibility == Visibility.Visible;
        }

    }
    
    public class HalfWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width)
            {
                return width / 2; // Return half of the window's width
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
