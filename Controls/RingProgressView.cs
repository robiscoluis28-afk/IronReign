using IronReign.Helpers;

namespace IronReign.Controls;

public class RingProgressView : GraphicsView
{
    private readonly RingProgressDrawable _drawable = new();

    public static readonly BindableProperty ProgressProperty = BindableProperty.Create(
        nameof(Progress), typeof(double), typeof(RingProgressView), 0.0, propertyChanged: OnProgressChanged);

    public double Progress
    {
        get => (double)GetValue(ProgressProperty);
        set => SetValue(ProgressProperty, value);
    }

    public static readonly BindableProperty ProgressColorProperty = BindableProperty.Create(
        nameof(ProgressColor), typeof(Color), typeof(RingProgressView), Colors.Orange, propertyChanged: OnColorChanged);

    public Color ProgressColor
    {
        get => (Color)GetValue(ProgressColorProperty);
        set => SetValue(ProgressColorProperty, value);
    }

    public static readonly BindableProperty TrackColorProperty = BindableProperty.Create(
        nameof(TrackColor), typeof(Color), typeof(RingProgressView), Colors.Gray, propertyChanged: OnColorChanged);

    public Color TrackColor
    {
        get => (Color)GetValue(TrackColorProperty);
        set => SetValue(TrackColorProperty, value);
    }

    public RingProgressView()
    {
        Drawable = _drawable;
    }

    private static void OnProgressChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (RingProgressView)bindable;
        view._drawable.Progress = (double)newValue;
        view.Invalidate();
    }

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (RingProgressView)bindable;
        view._drawable.ProgressColor = view.ProgressColor;
        view._drawable.TrackColor = view.TrackColor;
        view.Invalidate();
    }
}
