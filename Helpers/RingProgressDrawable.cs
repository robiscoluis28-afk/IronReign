namespace IronReign.Helpers;

public class RingProgressDrawable : IDrawable
{
    public double Progress { get; set; }

    public Color TrackColor { get; set; } = Colors.Gray;

    public Color ProgressColor { get; set; } = Colors.Orange;

    public float StrokeWidth { get; set; } = 6;

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        var size = Math.Min(dirtyRect.Width, dirtyRect.Height);
        var radius = (size - StrokeWidth) / 2;
        var centerX = dirtyRect.Width / 2;
        var centerY = dirtyRect.Height / 2;

        canvas.StrokeSize = StrokeWidth;
        canvas.StrokeLineCap = LineCap.Round;

        canvas.StrokeColor = TrackColor;
        canvas.DrawEllipse(centerX - radius, centerY - radius, radius * 2, radius * 2);

        var progress = Math.Clamp(Progress, 0, 1);

        if (progress <= 0)
            return;

        canvas.StrokeColor = ProgressColor;

        var startAngle = 90f;
        var endAngle = (float)(90 - (360 * progress));

        canvas.DrawArc(centerX - radius, centerY - radius, radius * 2, radius * 2, startAngle, endAngle, true, false);
    }
}
