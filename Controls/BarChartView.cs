using IronReign.Helpers;

namespace IronReign.Controls;

public class BarChartView : GraphicsView
{
    private readonly BarChartDrawable _drawable = new();

    public static readonly BindableProperty ValuesProperty = BindableProperty.Create(
        nameof(Values), typeof(IList<double>), typeof(BarChartView), new List<double>(), propertyChanged: OnValuesChanged);

    public IList<double> Values
    {
        get => (IList<double>)GetValue(ValuesProperty);
        set => SetValue(ValuesProperty, value);
    }

    public static readonly BindableProperty BarColorProperty = BindableProperty.Create(
        nameof(BarColor), typeof(Color), typeof(BarChartView), Colors.Gray, propertyChanged: OnColorChanged);

    public Color BarColor
    {
        get => (Color)GetValue(BarColorProperty);
        set => SetValue(BarColorProperty, value);
    }

    public BarChartView()
    {
        Drawable = _drawable;
    }

    private static void OnValuesChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (BarChartView)bindable;
        view._drawable.Values = new List<double>((IList<double>)newValue);
        view.Invalidate();
    }

    private static void OnColorChanged(BindableObject bindable, object oldValue, object newValue)
    {
        var view = (BarChartView)bindable;
        view._drawable.BarColor = view.BarColor;
        view.Invalidate();
    }
}
