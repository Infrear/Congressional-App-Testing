using System;
using System.Windows.Threading;

public enum EasingStyle
{
    Linear,
    Quadratic
}

public class Tween
{
    private readonly DispatcherTimer timer;
    private readonly double startValue;
    private readonly double endValue;
    private readonly double duration;
    private readonly EasingStyle easingStyle;
    private double elapsedTime;
    private Action<double> onUpdate;
    private Action onComplete;

    public event Action OnStop; 

    public Tween(double startValue, double endValue, double duration, EasingStyle easingStyle, Action<double> onUpdate, Action onComplete = null)
    {
        this.startValue = startValue;
        this.endValue = endValue;
        this.duration = duration;
        this.easingStyle = easingStyle;
        this.onUpdate = onUpdate;
        this.onComplete = onComplete;

        timer = new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(16) // Approx 60 FPS
        };
        timer.Tick += Update;
    }

    public void Start()
    {
        elapsedTime = 0;
        timer.Start();
    }

    public void Stop() // Method to stop the tween
    {
        timer.Stop();
        OnStop?.Invoke(); // Invoke the stop event
    }

    private void Update(object sender, EventArgs e)
    {
        elapsedTime += 16; // Increment elapsed time

        // Calculate progress
        double t = Math.Min(elapsedTime / duration, 1.0);
        double value = CalculateValue(t);

        // Update the value using the callback
        onUpdate(value);

        // Stop the timer when the animation is complete
        if (t >= 1.0)
        {
            timer.Stop();
            onComplete?.Invoke(); // Invoke the completion callback
            OnStop?.Invoke(); // Invoke the stop event
        }
    }

    private double CalculateValue(double t)
    {
        switch (easingStyle)
        {
            case EasingStyle.Linear:
                return startValue + (endValue - startValue) * t;

            case EasingStyle.Quadratic:
                return startValue + (endValue - startValue) * (t * t); // Quadratic easing

            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
